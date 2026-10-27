using System.Numerics;
using System.Text;
using Honeycomb.Formats.Fio;
using Honeycomb.Formats.Tpl;
using RedFox.GameExtraction;
using RedFox.GameExtraction.AssetHandlers;
using RedFox.Graphics3D;
using RedFox.Graphics3D.Buffers;
using RedFox.Graphics3D.Groups;
using RedFox.IO.FileSystem;

namespace Honeycomb.Handlers.Models;

public sealed class TplModelHandler : ModelHandler
{
    private const string	ResourceSuffix		= ".tpl.resource";
    private const string	TplSuffix			= ".tpl";
    private const string	HelperTexture		= "__default_material__";
    private const string	HiddenAffix			= "visibility_hidden";
    private const string	CollisionAffix		= "cdt_special_mesh";
    private const string	GrassCutterMaterial	= "grass_cutter";
    private const int		AllLods				= -1;

    public override bool CanHandle(Asset asset, GameExtractionConfiguration configuration)
    {
        return asset.Name.EndsWith(ResourceSuffix, StringComparison.OrdinalIgnoreCase) || asset.Name.EndsWith(TplSuffix, StringComparison.OrdinalIgnoreCase);
    }

    public override async Task<AssetReadResult> ReadAsync(Asset asset, AssetReadContext context, CancellationToken cancellationToken)
    {
        VirtualFile			resourceFile	= asset.DataSource as VirtualFile ?? throw new InvalidOperationException($"Asset '{asset.Name}' is not backed by a file.");
        TplResource			resource		= ReadResource(resourceFile, out VirtualDirectory directory);
        TplTemplate			template		= TplTemplate.Read(GetFile(directory, resource.TplFile).ReadAllBytes());
        string				modelName		= Path.GetFileNameWithoutExtension(resource.TplFile);
        int[]				splitLods		= GetSplitLods(template);
        int					lodCount		= splitLods.Length > 0 ? Math.Max(1, splitLods.Max() + 1) : 1;
        TplSplitGeometry?[]	splitGeometries	= new TplSplitGeometry?[splitLods.Length];
        bool				exportHelpers	= context.Configuration.GetOption("ExportHelperGeometry", false);
        List<Scene>			scenes			= [];

        if (template.Geometry.HasExternalStreams)
        {
            template.Geometry.LoadStreamData(GetFile(directory, resource.TplDataFile).ReadAllBytes());
        }

        IEnumerable<int> lods = context.Purpose == AssetReadPurpose.Thumbnail ? [lodCount / 2] : Enumerable.Range(0, lodCount);

        foreach (int lod in lods)
        {
            Scene scene = await CreateSceneAsync($"{modelName}_lod{lod}", template, splitLods, splitGeometries, lod, exportHelpers, context, cancellationToken);

            if (scene.EnumerateDescendants<Mesh>().Any())
            {
                scenes.Add(scene);
            }
        }

        return new AssetReadResult
        {
            Asset	= asset,
            Data	= scenes.ToArray(),
            Handler	= this,
        };
    }

    private static TplResource ReadResource(VirtualFile resourceFile, out VirtualDirectory directory)
    {
        directory = resourceFile.Parent ?? throw new InvalidOperationException($"'{resourceFile.Name}' is not mounted in a file system.");

        if (resourceFile.Name.EndsWith(ResourceSuffix, StringComparison.OrdinalIgnoreCase))
        {
            return TplResource.Parse(Encoding.UTF8.GetString(resourceFile.ReadAllBytes()));
        }

        byte[] data = resourceFile.ReadAllBytes();

        if (!TplDescriptor.IsDescriptor(data))
        {
            return TplResource.FromTpl(resourceFile.Name);
        }

        while (directory.Parent is not null)
        {
            directory = directory.Parent;
        }

        string tplFile = TplDescriptor.Read(data).Files.First(file => file is not null && file.EndsWith(TplSuffix, StringComparison.OrdinalIgnoreCase))!;

        return TplResource.FromTpl(tplFile.Replace('<', '[').Replace('>', ']').Replace('\\', '/'));
    }

    private static async Task<Scene> CreateSceneAsync(string name, TplTemplate template, int[] splitLods, TplSplitGeometry?[] splitGeometries, int lod, bool exportHelpers, AssetReadContext context, CancellationToken cancellationToken)
    {
        TplGeometry			geometry	= template.Geometry;
        Scene				scene		= new(name);
        MeshGroup			group		= scene.AddNode(new MeshGroup($"LOD{lod}"));
        Skeleton			skeleton	= group.AddNode(new Skeleton($"{name}_skeleton"));
        SkeletonBone[]		bones		= CreateBones(geometry, skeleton);
        TplMaterialBuilder	materials	= new(group, context);
        bool				boneSpace	= template.Skin is { HasInverseBindMatrices: false };
        int					meshCount	= 0;

        for (int i = 0; i < geometry.Splits.Count; i++)
        {
            TplSplit split = geometry.Splits[i];

            if ((splitLods[i] != AllLods && splitLods[i] != lod) || split.FaceCount == 0 || (!exportHelpers && IsHelper(geometry, split)))
            {
                continue;
            }

            TplSplitGeometry	splitGeometry	= splitGeometries[i] ??= TplSplitGeometry.Read(geometry, split);
            Mesh				mesh			= group.AddNode<Mesh>($"{name}_mesh{meshCount}");

            meshCount++;

            WriteVertices(mesh, geometry, split, splitGeometry, boneSpace);

            if (splitGeometry.InfluenceCount > 0)
            {
                mesh.Skin = new Skin(bones, new DataBuffer<int>(splitGeometry.BoneIndices, splitGeometry.InfluenceCount, 1), new DataBuffer<float>(splitGeometry.BoneWeights, splitGeometry.InfluenceCount, 1));
            }

            mesh.FaceIndices	= new DataBuffer<int>(splitGeometry.Faces, 1, 1);
            mesh.Materials		= [await materials.GetMaterialAsync(split.Material, cancellationToken)];
        }

        return scene;
    }

    private static bool IsHelper(TplGeometry geometry, TplSplit split)
    {
        FioPropertySection?	layer	= split.Material?.GetSection("layer0");
        string[]			affixes	= geometry.Objects[split.ObjectIndex].Affixes.Split('\n', StringSplitOptions.TrimEntries);

        return layer?.GetString("texName") == HelperTexture || layer?.GetString("mtlName") == GrassCutterMaterial || affixes.Contains(HiddenAffix) || affixes.Contains(CollisionAffix);
    }

    private static SkeletonBone[] CreateBones(TplGeometry geometry, Skeleton skeleton)
    {
        Dictionary<int, string>	names	= [];
        SkeletonBone[]			bones	= new SkeletonBone[geometry.Objects.Count];
        int[]					parents	= new int[geometry.Objects.Count];

        for (int i = 0; i < geometry.ObjectIds.Length; i++)
        {
            if (geometry.Names[i] is string name)
            {
                names[geometry.ObjectIds[i]] = name;
            }
        }

        for (int i = 0; i < bones.Length; i++)
        {
            Matrix4x4.Decompose(geometry.GetLocalMatrix(i), out Vector3 scale, out Quaternion rotation, out Vector3 translation);

            bones[i] = new SkeletonBone(names.GetValueOrDefault(i, $"object_{i}"));
            bones[i].BindTransform.LocalPosition	= translation;
            bones[i].BindTransform.LocalRotation	= rotation;
            bones[i].BindTransform.Scale			= scale;
            parents[i]								= geometry.Objects[i].Parent == TplObject.NoObject ? -1 : geometry.Objects[i].Parent;
        }

        skeleton.LinkHierarchyUnsafe(bones, parents);
        return bones;
    }

    private static void WriteVertices(Mesh mesh, TplGeometry geometry, TplSplit split, TplSplitGeometry splitGeometry, bool boneSpace)
    {
        int			vertexCount	= splitGeometry.Positions.Length;
        float[]		positions	= new float[vertexCount * 3];
        float[]		normals		= new float[vertexCount * 3];
        float[]		texCoords	= new float[vertexCount * splitGeometry.TexCoordLayers.Count * 2];
        Matrix4x4	transform	= geometry.GetObjectMatrix(split.ObjectIndex);

        for (int i = 0; i < vertexCount; i++)
        {
            if (boneSpace && splitGeometry.InfluenceCount > 0)
            {
                transform = GetBoneSpaceTransform(geometry, splitGeometry, i);
            }

            Vector3 position	= Vector3.Transform(splitGeometry.Positions[i], transform);
            Vector3 normal		= splitGeometry.Normals is null ? Vector3.Zero : Vector3.Normalize(Vector3.TransformNormal(splitGeometry.Normals[i], transform));

            position.CopyTo(positions, i * 3);
            normal.CopyTo(normals, i * 3);

            for (int layer = 0; layer < splitGeometry.TexCoordLayers.Count; layer++)
            {
                splitGeometry.TexCoordLayers[layer][i].CopyTo(texCoords, (i * splitGeometry.TexCoordLayers.Count + layer) * 2);
            }
        }

        mesh.Positions = new DataBuffer<float>(positions, 1, 3);

        if (splitGeometry.Normals is not null)
        {
            mesh.Normals = new DataBuffer<float>(normals, 1, 3);
        }

        if (splitGeometry.TexCoordLayers.Count > 0)
        {
            mesh.UVLayers = new DataBuffer<float>(texCoords, splitGeometry.TexCoordLayers.Count, 2);
        }
    }

    private static Matrix4x4 GetBoneSpaceTransform(TplGeometry geometry, TplSplitGeometry splitGeometry, int vertex)
    {
        Matrix4x4 transform = new();

        for (int i = 0; i < splitGeometry.InfluenceCount; i++)
        {
            int index = vertex * splitGeometry.InfluenceCount + i;

            transform += geometry.GetObjectMatrix(splitGeometry.BoneIndices[index]) * splitGeometry.BoneWeights[index];
        }

        return transform;
    }

    private static int[] GetSplitLods(TplTemplate template)
    {
        Dictionary<int, int>	objectLods	= [];
        int[]					splitLods	= new int[template.Geometry.Splits.Count];

        foreach (TplLodDefinition definition in template.LodDefinitions)
        {
            objectLods[definition.ObjectId] = definition.Lod;
        }

        foreach (TplLodRoot root in template.Geometry.LodRoots)
        {
            int start = 0;

            for (int lod = 0; lod < root.LodEnds.Length; lod++)
            {
                for (int i = start; i < root.LodEnds[lod]; i++)
                {
                    objectLods[root.ObjectIds[i]] = lod;
                }

                start = root.LodEnds[lod];
            }
        }

        for (int i = 0; i < splitLods.Length; i++)
        {
            TplSplit split = template.Geometry.Splits[i];

            splitLods[i] = FindLod(template.Geometry, objectLods, (ushort)split.ObjectIndex);

            if (splitLods[i] == AllLods)
            {
                splitLods[i] = FindLod(template.Geometry, objectLods, split.SkinCompoundId);
            }
        }

        return splitLods;
    }

    private static int FindLod(TplGeometry geometry, Dictionary<int, int> objectLods, ushort objectId)
    {
        while (objectId != TplObject.NoObject)
        {
            if (objectLods.TryGetValue(objectId, out int lod))
            {
                return lod;
            }

            objectId = geometry.Objects[objectId].Parent;
        }

        return AllLods;
    }

    private static VirtualFile GetFile(VirtualDirectory directory, string name)
    {
        if (!directory.TryGetFile(name, out VirtualFile? file))
        {
            throw new FileNotFoundException($"'{directory.FullPath}/{name}' was not found, load the pak that contains it.", name);
        }

        return file;
    }
}
