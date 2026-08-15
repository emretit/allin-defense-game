#!/usr/bin/env python3
"""Generate .meta for all scripts, placeholder art, and Resources prefabs for Defender Chronicles."""
import hashlib
import os
import struct
import zlib

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
ASSETS = os.path.join(ROOT, "Assets")
SCRIPTS = os.path.join(ASSETS, "Scripts")
ART = os.path.join(ASSETS, "Art")
RES = os.path.join(ASSETS, "Resources", "Prefabs")


def guid_for_path(rel_path: str) -> str:
    h = hashlib.md5(rel_path.encode()).hexdigest()
    return h[:32]


def write_meta(path: str, guid: str, importer: str = "MonoImporter", extra: str = ""):
    meta = f"""fileFormatVersion: 2
guid: {guid}
{importer}:
  externalObjects: {{}}
  serializedVersion: 2
  defaultReferences: []
  executionOrder: 0
  icon: {{instanceID: 0}}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
{extra}"""
    with open(path + ".meta", "w") as f:
        f.write(meta)


def write_folder_meta(folder: str, guid: str):
    meta = f"""fileFormatVersion: 2
guid: {guid}
folderAsset: yes
DefaultImporter:
  externalObjects: {{}}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""
    with open(os.path.join(folder, ".meta"), "w") as f:
        f.write(meta)


def png_white(path: str, w: int = 32, h: int = 32):
    """Minimal RGBA PNG."""
    raw = b"".join(
        b"\xff\xff\xff\xff" * w for _ in range(h)
    )
    def chunk(tag, data):
        return struct.pack(">I", len(data)) + tag + data + struct.pack(">I", zlib.crc32(tag + data) & 0xFFFFFFFF)
    ihdr = struct.pack(">IIBBBBB", w, h, 8, 6, 0, 0, 0)
    png = b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", ihdr) + chunk(b"IDATA", zlib.compress(raw, 9)) + chunk(b"IEND", b"")
    with open(path, "wb") as f:
        f.write(png)


def texture_meta(guid: str):
    return f"""fileFormatVersion: 2
guid: {guid}
TextureImporter:
  internalIDToNameTable:
  - first:
      213: {guid}21300000
    second: White
  externalObjects: {{}}
  serializedVersion: 13
  mipmaps:
    mipMapMode: 0
    enableMipMap: 0
    sRGBTexture: 1
    linearTexture: 0
    fadeOut: 0
    borderMipMap: 0
    mipMapsPreserveCoverage: 0
    alphaTestReferenceValue: 0.5
    mipMapFadeDistanceStart: 1
    mipMapFadeDistanceEnd: 3
  bumpmap:
    convertToNormalMap: 0
    externalNormalMap: 0
    heightScale: 0.25
    normalMapFilter: 0
    flipGreenChannel: 0
  isReadable: 0
  streamingMipmaps: 0
  streamingMipmapsPriority: 0
  vTOnly: 0
  ignoreMipmapLimit: 0
  grayScaleToAlpha: 0
  generateCubemap: 6
  cubemapConvolution: 0
  seamlessCubemap: 0
  textureFormat: -1
  maxTextureSize: 2048
  textureSettings:
    serializedVersion: 2
    filterMode: 0
    aniso: 1
    mipBias: 0
    wrapU: 1
    wrapV: 1
    wrapW: 0
  nPOTScale: 0
  lightmap: 0
  compressionQuality: 50
  spriteMode: 1
  spriteExtrude: 1
  spriteMeshType: 1
  alignment: 0
  spritePivot: {{x: 0.5, y: 0.5}}
  spritePixelsToUnits: 100
  spriteBorder: {{x: 0, y: 0, z: 0, w: 0}}
  spriteGenerateFallbackPhysicsShape: 1
  alphaUsage: 1
  alphaIsTransparency: 1
  spriteTessellationDetail: -1
  textureType: 8
  textureShape: 1
  singleChannelComponent: 0
  flipbookRows: 1
  flipbookColumns: 1
  maxTextureSizeSet: 0
  compressionQualitySet: 0
  textureFormatSet: 0
  ignorePngGamma: 0
  applyGammaDecoding: 0
  platformSettings:
  - serializedVersion: 4
    buildTarget: DefaultTexturePlatform
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 0
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  spriteSheet:
    serializedVersion: 2
    sprites:
    - serializedVersion: 2
      name: White
      rect:
        serializedVersion: 2
        x: 0
        y: 0
        width: 32
        height: 32
      alignment: 0
      pivot: {{x: 0.5, y: 0.5}}
      border: {{x: 0, y: 0, z: 0, w: 0}}
      outline: []
      physicsShape: []
      tessellationDetail: -1
      bones: []
      spriteID: 
      internalID: 0
      vertices: []
      indices: 
      edges: []
      weights: []
    outline: []
    physicsShape: []
    bones: []
    spriteID: 
    internalID: 0
    vertices: []
    indices: 
    edges: []
    weights: []
    secondaryTextures: []
    nameFileIdTable: {{}}
  spritePackingTag: 
  pSDRemoveMatte: 0
  pSDShowRemoveMatteOption: 0
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""


def main():
    os.makedirs(ART, exist_ok=True)
    os.makedirs(os.path.join(RES, "Enemies"), exist_ok=True)
    os.makedirs(os.path.join(RES, "Towers"), exist_ok=True)
    os.makedirs(os.path.join(RES, "Projectiles"), exist_ok=True)
    os.makedirs(os.path.join(ASSETS, "Resources"), exist_ok=True)

    # Folder metas
    for folder, rel in [
        (ASSETS, "Assets"),
        (SCRIPTS, "Assets/Scripts"),
        (ART, "Assets/Art"),
        (os.path.join(ASSETS, "Resources"), "Assets/Resources"),
        (RES, "Assets/Resources/Prefabs"),
        (os.path.join(RES, "Enemies"), "Assets/Resources/Prefabs/Enemies"),
        (os.path.join(RES, "Towers"), "Assets/Resources/Prefabs/Towers"),
        (os.path.join(RES, "Projectiles"), "Assets/Resources/Prefabs/Projectiles"),
    ]:
        write_folder_meta(folder, guid_for_path(rel))

    # All C# metas
    for dirpath, _, files in os.walk(SCRIPTS):
        for fn in files:
            if not fn.endswith(".cs"):
                continue
            path = os.path.join(dirpath, fn)
            rel = os.path.relpath(path, ROOT).replace(os.sep, "/")
            g = guid_for_path(rel)
            write_meta(path, g)

    # White PNG + texture meta
    png_path = os.path.join(ART, "White32.png")
    png_white(png_path)
    tex_guid = guid_for_path("Assets/Art/White32.png")
    with open(png_path + ".meta", "w") as f:
        f.write(texture_meta(tex_guid))

    # Script GUIDs (for prefab YAML)
    def sg(script_rel):
        return guid_for_path(script_rel)

    scripts = {
        "GoblinEnemy": sg("Assets/Scripts/Enemies/GoblinEnemy.cs"),
        "OrcEnemy": sg("Assets/Scripts/Enemies/OrcEnemy.cs"),
        "TrollEnemy": sg("Assets/Scripts/Enemies/TrollEnemy.cs"),
        "DragonEnemy": sg("Assets/Scripts/Enemies/DragonEnemy.cs"),
        "ArcherTower": sg("Assets/Scripts/Towers/ArcherTower.cs"),
        "MageTower": sg("Assets/Scripts/Towers/MageTower.cs"),
        "WarriorTower": sg("Assets/Scripts/Towers/WarriorTower.cs"),
        "CatapultTower": sg("Assets/Scripts/Towers/CatapultTower.cs"),
        "ArrowProjectile": sg("Assets/Scripts/Projectiles/ArrowProjectile.cs"),
        "FireballProjectile": sg("Assets/Scripts/Projectiles/FireballProjectile.cs"),
    }

    sprite_ref = f"{{fileID: 21300000, guid: {tex_guid}, type: 3}}"

    def prefab_meta(guid: str):
        return f"""fileFormatVersion: 2
guid: {guid}
PrefabImporter:
  externalObjects: {{}}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""

    def enemy_yaml(name, script_key, layer, health, speed, gold, dmg, extra_yaml=""):
        sc = scripts[script_key]
        guid = guid_for_path(f"Assets/Resources/Prefabs/Enemies/{name}.prefab")
        y = f"""%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!1 &100000
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  serializedVersion: 6
  m_Component:
  - component: {{fileID: 100001}}
  - component: {{fileID: 100002}}
  - component: {{fileID: 100003}}
  - component: {{fileID: 100004}}
  m_Layer: {layer}
  m_Name: {name}
  m_TagString: Untagged
  m_Icon: {{fileID: 0}}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &100001
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 100000}}
  serializedVersion: 2
  m_LocalRotation: {{x: 0, y: 0, z: 0, w: 1}}
  m_LocalPosition: {{x: 0, y: 0, z: 0}}
  m_LocalScale: {{x: 1, y: 1, z: 1}}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {{fileID: 0}}
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}
--- !u!212 &100002
SpriteRenderer:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 100000}}
  m_Enabled: 1
  m_CastShadows: 0
  m_ReceiveShadows: 0
  m_DynamicOccludee: 1
  m_StaticShadowCaster: 0
  m_MotionVectors: 1
  m_LightProbeUsage: 1
  m_ReflectionProbeUsage: 1
  m_RayTracingMode: 0
  m_RayTraceProcedural: 0
  m_RenderingLayerMask: 1
  m_RendererPriority: 0
  m_Materials:
  - {{fileID: 10754, guid: 0000000000000000f000000000000000, type: 0}}
  m_StaticBatchInfo:
    firstSubMesh: 0
    subMeshCount: 0
  m_StaticBatchRoot: {{fileID: 0}}
  m_ProbeAnchor: {{fileID: 0}}
  m_LightProbeVolumeOverride: {{fileID: 0}}
  m_ScaleInLightmap: 1
  m_ReceiveGI: 1
  m_PreserveUVs: 0
  m_IgnoreNormalsForChartDetection: 0
  m_ImportantGI: 0
  m_StitchLightmapSeams: 1
  m_SelectedEditorRenderState: 0
  m_MinimumChartSize: 4
  m_AutoUVMaxDistance: 0.5
  m_AutoUVMaxAngle: 89
  m_LightmapParameters: {{fileID: 0}}
  m_SortingLayerID: 0
  m_SortingLayer: 0
  m_SortingOrder: 0
  m_Sprite: {sprite_ref}
  m_Color: {{r: 1, g: 1, b: 1, a: 1}}
  m_FlipX: 0
  m_FlipY: 0
  m_DrawMode: 0
  m_Size: {{x: 0.32, y: 0.32}}
  m_AdaptiveModeThreshold: 0.5
  m_SpriteTileMode: 0
  m_WasSpriteAssigned: 1
  m_MaskInteraction: 0
  m_SpriteSortPoint: 0
--- !u!58 &100003
CircleCollider2D:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 100000}}
  m_Enabled: 1
  m_Density: 1
  m_Material: {{fileID: 0}}
  m_IncludeLayers:
    serializedVersion: 2
    m_Bits: 0
  m_ExcludeLayers:
    serializedVersion: 2
    m_Bits: 0
  m_LayerOverridePriority: 0
  m_ForceSendLayers:
    serializedVersion: 2
    m_Bits: 4294967295
  m_ForceReceiveLayers:
    serializedVersion: 2
    m_Bits: 4294967295
  m_ContactCaptureLayers:
    serializedVersion: 2
    m_Bits: 4294967295
  m_CallbackLayers:
    serializedVersion: 2
    m_Bits: 4294967295
  m_IsTrigger: 0
  m_UsedByEffector: 0
  m_CompositeOperation: 0
  m_CompositeOrder: 0
  m_Offset: {{x: 0, y: 0}}
  m_Radius: 0.35
--- !u!114 &100004
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 100000}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {sc}, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: 
  enemyData:
    enemyName: {name}
    enemyType: 0
    maxHealth: {health}
    moveSpeed: {speed}
    goldReward: {gold}
    damage: {dmg}
    sprite: {{fileID: 0}}
  spriteRenderer: {{fileID: 100002}}
  healthBarPrefab: {{fileID: 0}}
{extra_yaml}
"""
        path = os.path.join(RES, "Enemies", f"{name}.prefab")
        with open(path, "w") as f:
            f.write(y)
        with open(path + ".meta", "w") as f:
            f.write(prefab_meta(guid))
        return guid

    # Enemy types: Goblin=0 Orc=1 Troll=2 Dragon=3
    enemy_yaml("GoblinEnemy", "GoblinEnemy", 8, 60, 2.5, 10, 1, "  dodgeChance: 0.15\n")
    # Fix Orc - need enemyType 1 in YAML - use custom
    orc_guid = guid_for_path("Assets/Resources/Prefabs/Enemies/OrcEnemy.prefab")
    orc = enemy_yaml("OrcEnemy", "OrcEnemy", 8, 200, 1.8, 20, 1, "  berserkHealthThreshold: 0.3\n  berserkSpeedMultiplier: 2\n")
    # Patch Orc enemyType - the template uses 0; write Orc manually
    orc_path = os.path.join(RES, "Enemies", "OrcEnemy.prefab")
    with open(orc_path) as f:
        oc = f.read()
    oc = oc.replace("enemyType: 0", "enemyType: 1", 1)
    with open(orc_path, "w") as f:
        f.write(oc)

    troll_path = os.path.join(RES, "Enemies", "TrollEnemy.prefab")
    enemy_yaml("TrollEnemy", "TrollEnemy", 8, 500, 0.8, 40, 2, "  regenAmountPerSecond: 5\n  regenInterval: 1\n")
    with open(troll_path) as f:
        tr = f.read()
    tr = tr.replace("enemyType: 0", "enemyType: 2", 1)
    with open(troll_path, "w") as f:
        f.write(tr)

    enemy_yaml("DragonEnemy", "DragonEnemy", 8, 1000, 1.2, 100, 3,
               "  fireBreathRadius: 2\n  fireBreathDamage: 10\n  fireBreathCooldown: 5\n  towerLayer:\n    serializedVersion: 2\n    m_Bits: 512\n")
    dr_path = os.path.join(RES, "Enemies", "DragonEnemy.prefab")
    with open(dr_path) as f:
        dr = f.read()
    dr = dr.replace("enemyType: 0", "enemyType: 3", 1)
    with open(dr_path, "w") as f:
        f.write(dr)

    # Goblin enemyType 0 ok

    def proj_yaml(name, script_key, color):
        sc = scripts[script_key]
        guid = guid_for_path(f"Assets/Resources/Prefabs/Projectiles/{name}.prefab")
        y = f"""%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!1 &100000
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  serializedVersion: 6
  m_Component:
  - component: {{fileID: 100001}}
  - component: {{fileID: 100002}}
  m_Layer: 0
  m_Name: {name}
  m_TagString: Untagged
  m_Icon: {{fileID: 0}}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &100001
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 100000}}
  serializedVersion: 2
  m_LocalRotation: {{x: 0, y: 0, z: 0, w: 1}}
  m_LocalPosition: {{x: 0, y: 0, z: 0}}
  m_LocalScale: {{x: 0.5, y: 0.5, z: 1}}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {{fileID: 0}}
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}
--- !u!212 &100002
SpriteRenderer:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 100000}}
  m_Enabled: 1
  m_CastShadows: 0
  m_ReceiveShadows: 0
  m_DynamicOccludee: 1
  m_StaticShadowCaster: 0
  m_MotionVectors: 1
  m_LightProbeUsage: 1
  m_ReflectionProbeUsage: 1
  m_RayTracingMode: 0
  m_RayTraceProcedural: 0
  m_RenderingLayerMask: 1
  m_RendererPriority: 0
  m_Materials:
  - {{fileID: 10754, guid: 0000000000000000f000000000000000, type: 0}}
  m_StaticBatchInfo:
    firstSubMesh: 0
    subMeshCount: 0
  m_StaticBatchRoot: {{fileID: 0}}
  m_ProbeAnchor: {{fileID: 0}}
  m_LightProbeVolumeOverride: {{fileID: 0}}
  m_ScaleInLightmap: 1
  m_ReceiveGI: 1
  m_PreserveUVs: 0
  m_IgnoreNormalsForChartDetection: 0
  m_ImportantGI: 0
  m_StitchLightmapSeams: 1
  m_SelectedEditorRenderState: 0
  m_MinimumChartSize: 4
  m_AutoUVMaxDistance: 0.5
  m_AutoUVMaxAngle: 89
  m_LightmapParameters: {{fileID: 0}}
  m_SortingLayerID: 0
  m_SortingLayer: 0
  m_SortingOrder: 5
  m_Sprite: {sprite_ref}
  m_Color: {color}
  m_FlipX: 0
  m_FlipY: 0
  m_DrawMode: 0
  m_Size: {{x: 0.2, y: 0.2}}
  m_AdaptiveModeThreshold: 0.5
  m_SpriteTileMode: 0
  m_WasSpriteAssigned: 1
  m_MaskInteraction: 0
  m_SpriteSortPoint: 0
--- !u!114 &100003
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 100000}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {sc}, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: 
  speed: 12
  lifeTime: 5
"""
        if script_key == "ArrowProjectile":
            y += "  trail: {fileID: 0}\n"
        else:
            y += """  splashRadius: 1.5
  enemyLayer:
    serializedVersion: 2
    m_Bits: 256
  explosionEffect: {fileID: 0}
"""
        path = os.path.join(RES, "Projectiles", f"{name}.prefab")
        y = y.replace(
            "m_Component:\n  - component: {fileID: 100001}\n  - component: {fileID: 100002}",
            "m_Component:\n  - component: {fileID: 100001}\n  - component: {fileID: 100002}\n  - component: {fileID: 100003}",
        )
        with open(path, "w") as f:
            f.write(y)
        with open(path + ".meta", "w") as f:
            f.write(prefab_meta(guid))

    proj_yaml("ArrowProjectile", "ArrowProjectile", "{r: 0.8, g: 0.6, b: 0.2, a: 1}")
    proj_yaml("FireballProjectile", "FireballProjectile", "{r: 1, g: 0.3, b: 0, a: 1}")

    # Tower prefabs (layer 9 = Tower)
    def tower_yaml(name, script_key, ttype, cost, range_, aps, dmg, projectile_path, extra=""):
        sc = scripts[script_key]
        guid = guid_for_path(f"Assets/Resources/Prefabs/Towers/{name}.prefab")
        proj_guid = ""
        proj_fileid = "{fileID: 0}"
        if projectile_path:
            proj_guid = guid_for_path(projectile_path)
            proj_fileid = f"{{fileID: 100000, guid: {proj_guid}, type: 3}}"
        fire_y = 0.15
        y = f"""%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!1 &200000
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  serializedVersion: 6
  m_Component:
  - component: {{fileID: 200001}}
  - component: {{fileID: 200002}}
  - component: {{fileID: 200005}}
  m_Layer: 9
  m_Name: {name}
  m_TagString: Untagged
  m_Icon: {{fileID: 0}}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &200001
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 200000}}
  serializedVersion: 2
  m_LocalRotation: {{x: 0, y: 0, z: 0, w: 1}}
  m_LocalPosition: {{x: 0, y: 0, z: 0}}
  m_LocalScale: {{x: 1, y: 1, z: 1}}
  m_ConstrainProportionsScale: 0
  m_Children:
  - {{fileID: 200003}}
  m_Father: {{fileID: 0}}
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}
--- !u!212 &200002
SpriteRenderer:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 200000}}
  m_Enabled: 1
  m_CastShadows: 0
  m_ReceiveShadows: 0
  m_DynamicOccludee: 1
  m_StaticShadowCaster: 0
  m_MotionVectors: 1
  m_LightProbeUsage: 1
  m_ReflectionProbeUsage: 1
  m_RayTracingMode: 0
  m_RayTraceProcedural: 0
  m_RenderingLayerMask: 1
  m_RendererPriority: 0
  m_Materials:
  - {{fileID: 10754, guid: 0000000000000000f000000000000000, type: 0}}
  m_StaticBatchInfo:
    firstSubMesh: 0
    subMeshCount: 0
  m_StaticBatchRoot: {{fileID: 0}}
  m_ProbeAnchor: {{fileID: 0}}
  m_LightProbeVolumeOverride: {{fileID: 0}}
  m_ScaleInLightmap: 1
  m_ReceiveGI: 1
  m_PreserveUVs: 0
  m_IgnoreNormalsForChartDetection: 0
  m_ImportantGI: 0
  m_StitchLightmapSeams: 1
  m_SelectedEditorRenderState: 0
  m_MinimumChartSize: 4
  m_AutoUVMaxDistance: 0.5
  m_AutoUVMaxAngle: 89
  m_LightmapParameters: {{fileID: 0}}
  m_SortingLayerID: 0
  m_SortingLayer: 0
  m_SortingOrder: 0
  m_Sprite: {sprite_ref}
  m_Color: {{r: 0.9, g: 0.9, b: 0.9, a: 1}}
  m_FlipX: 0
  m_FlipY: 0
  m_DrawMode: 0
  m_Size: {{x: 0.4, y: 0.4}}
  m_AdaptiveModeThreshold: 0.5
  m_SpriteTileMode: 0
  m_WasSpriteAssigned: 1
  m_MaskInteraction: 0
  m_SpriteSortPoint: 0
--- !u!1 &200004
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  serializedVersion: 6
  m_Component:
  - component: {{fileID: 200003}}
  m_Layer: 9
  m_Name: FirePoint
  m_TagString: Untagged
  m_Icon: {{fileID: 0}}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &200003
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 200004}}
  serializedVersion: 2
  m_LocalRotation: {{x: 0, y: 0, z: 0, w: 1}}
  m_LocalPosition: {{x: 0, y: {fire_y}, z: 0}}
  m_LocalScale: {{x: 1, y: 1, z: 1}}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {{fileID: 200001}}
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}
--- !u!114 &200005
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 200000}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {sc}, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: 
  towerData:
    towerName: {name}
    towerType: {ttype}
    cost: {cost}
    upgradeCost: 40
    attackRange: {range_}
    attackSpeed: {aps}
    damage: {dmg}
    level: 1
    projectileType: 0
    icon: {{fileID: 0}}
  firePoint: {{fileID: 200003}}
  towerRenderer: {{fileID: 200002}}
  projectilePrefab: {proj_fileid}
  enemyLayer:
    serializedVersion: 2
    m_Bits: 256
{extra}
"""
        path = os.path.join(RES, "Towers", f"{name}.prefab")
        with open(path, "w") as f:
            f.write(y)
        with open(path + ".meta", "w") as f:
            f.write(prefab_meta(guid))

    # Archer 0, Mage 1, Warrior 2, Catapult 3
    arrow_path = "Assets/Resources/Prefabs/Projectiles/ArrowProjectile.prefab"
    fireball_path = "Assets/Resources/Prefabs/Projectiles/FireballProjectile.prefab"

    tower_yaml("ArcherTower", "ArcherTower", 0, 75, 4, 1.2, 20, arrow_path,
               "  critChance: 0.1\n  critMultiplier: 2\n")
    tower_yaml("MageTower", "MageTower", 1, 125, 5, 0.8, 35, "",
               "  splashRadius: 1.5\n  slowFactor: 0.5\n  slowDuration: 2\n  enemyLayer:\n    serializedVersion: 2\n    m_Bits: 256\n")
    tower_yaml("WarriorTower", "WarriorTower", 2, 100, 2, 1.5, 50, "",
               "  blockChance: 0.2\n  auraSlowRadius: 2\n  auraSlow: 0.7\n  enemyLayer:\n    serializedVersion: 2\n    m_Bits: 256\n")
    tower_yaml("CatapultTower", "CatapultTower", 3, 150, 6, 0.5, 80, fireball_path,
               "  explosionRadius: 2\n  stunDuration: 1\n  enemyLayer:\n    serializedVersion: 2\n    m_Bits: 256\n")

    print("Generated meta, art, and Resources prefabs OK.")


if __name__ == "__main__":
    main()
