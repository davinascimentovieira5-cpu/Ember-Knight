#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace EmberKnight.EditorTools
{
    /// <summary>
    /// Faz o slice automático de Assets/EmberKnight/Sprites/ember_knight_atlas.png
    /// em um grid de 132x102 (8 colunas x 7 linhas), nomeando cada sprite
    /// como "linha_coluna" para bater com o layout ANIM do jogo original.
    ///
    /// Como usar:
    /// 1) Selecione a textura no Project (ou não precisa, o menu já acha o arquivo).
    /// 2) Menu: Ember Knight > Slice Sprite Atlas.
    /// 3) A textura precisa estar com Texture Type = Sprite (2D and UI) e
    ///    Sprite Mode = Multiple (o script já ajusta isso).
    ///
    /// Requer o pacote "2D Sprite" (com.unity.2d.sprite), que já vem
    /// habilitado por padrão em projetos criados com o template 2D.
    /// </summary>
    public static class EmberKnightSpriteSlicer
    {
        const string AtlasPath = "Assets/EmberKnight/Sprites/ember_knight_atlas.png";
        const int FrameW = 132;
        const int FrameH = 102;
        const int Cols = 8;
        const int Rows = 7;

        // Nome de cada linha, só para os sprites ficarem legíveis no Project.
        static readonly string[] RowNames =
        {
            "idle", "run", "attack", "special_jump_fall_land_portal", "damage", "death", "interact"
        };

        [MenuItem("Ember Knight/Slice Sprite Atlas")]
        public static void SliceAtlas()
        {
            var importer = AssetImporter.GetAtPath(AtlasPath) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError($"Não encontrei a textura em {AtlasPath}. Confira se o PNG foi importado.");
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.filterMode = FilterMode.Point;   // pixel art nítida
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.spritePixelsPerUnit = 100;

            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(AtlasPath);
            if (texture == null)
            {
                Debug.LogError("Falha ao carregar a textura para leitura de dimensões.");
                return;
            }

            var metas = new List<SpriteMetaData>();

            for (int row = 0; row < Rows; row++)
            {
                for (int col = 0; col < Cols; col++)
                {
                    // Unity conta Y de baixo pra cima; nossa linha 0 é o topo da imagem.
                    int yFromBottom = texture.height - (row + 1) * FrameH;

                    var meta = new SpriteMetaData
                    {
                        name = $"{RowNames[row]}_{col}",
                        rect = new Rect(col * FrameW, yFromBottom, FrameW, FrameH),
                        alignment = (int)SpriteAlignment.Custom,
                        pivot = new Vector2(0.5f, 0.08f) // pivô perto dos pés, bom p/ personagens 2D
                    };
                    metas.Add(meta);
                }
            }

            importer.spritesheet = metas.ToArray();
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();

            Debug.Log($"Ember Knight: {metas.Count} sprites gerados a partir de {AtlasPath}. " +
                      "Abra a textura no Project para ver os frames individuais " +
                      "(nome padrão: <animacao>_<coluna>).");
        }
    }
}
#endif
