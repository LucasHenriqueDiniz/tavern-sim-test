using System;
using System.Collections.Generic;
using System.IO;
using Unity.VectorGraphics;
using UnityEngine;
using UnityEngine.UIElements;

namespace TavernSim.UI
{
    /// <summary>
    /// Gerencia o carregamento e aplicação de ícones SVG nos botões do HUD.
    /// </summary>
    public static class IconManager
    {
        private const int IconResolution = 64;

        private static readonly Dictionary<string, Texture2D> _iconCache = new Dictionary<string, Texture2D>();
        private static Material _vectorMaterial;
        private static bool _vectorShaderUnavailable;
        
        // Mapeamento de botões para ícones
        private static readonly Dictionary<string, string> _buttonIconMap = new Dictionary<string, string>
        {
            { "messagesBtn", "chat-bubble" },
            { "statsBtn", "histogram" },
            { "infoBtn", "info" },
            { "panelToggleBtn", "expand" },
            { "panelPinBtn", "pin" },
            { "staffBtn", "cook" },
            { "pauseBtn", "pause-button" },
            { "play1Btn", "play-button" },
            { "play2Btn", "fast-forward-button" },
            { "play4Btn", "fast-forward-button" },
            { "saveBtn", "save" },
            { "loadBtn", "load" },
            { "buildMenuBtn", "build" },
            { "buildStructuresBtn", "bricks" },
            { "buildStairsBtn", "3d-stairs" },
            { "buildDoorsBtn", "entry-door" },
            { "buildDemolishBtn", "wrecking-ball" },
            { "decorMenuBtn", "large-paint-brush" },
            { "decorArtBtn", "mona-lisa" },
            { "decorBasicBtn", "house" },
            { "decorThemeBtn", "carnival-mask" },
            { "decorNatureBtn", "shiny-apple" },
            { "inventoryBtn", "beer-stein" },
            { "managementStaffBtn", "cook" },
            { "financesBtn", "money-stack" },
            { "eventsBtn", "drama-masks" },
            { "questsBtn", "contract" },
            { "buildToggleBtn", "build" },
            { "decoToggleBtn", "large-paint-brush" },
            { "beautyToggleBtn", "mona-lisa" },
            { "staffToggleBtn", "cook" }
        };

        private static readonly Dictionary<string, string> _elementIconMap = new Dictionary<string, string>
        {
            { "goldIcon", "two-coins" },
            { "reputationIcon", "round-star" },
            { "customersIcon", "hot-meal" },
            { "quickReputationIcon", "round-star" },
            { "quickCleanlinessIcon", "trash-can" },
            { "quickSatisfactionIcon", "thumb-up" },
            { "tavernBadge", "mounted-knight" }
        };

        /// <summary>
        /// Aplica ícones aos botões do HUD.
        /// </summary>
        public static void ApplyIconsToHUD(VisualElement rootElement)
        {
            if (rootElement == null) return;

            foreach (var mapping in _buttonIconMap)
            {
                var button = rootElement.Q<Button>(mapping.Key);
                if (button != null)
                {
                    ApplyIconToButton(button, mapping.Value);
                }
            }

            foreach (var mapping in _elementIconMap)
            {
                var element = rootElement.Q<VisualElement>(mapping.Key);
                if (element != null)
                {
                    ApplyIconToElement(element, mapping.Value);
                }
            }
        }

        /// <summary>
        /// Aplica um ícone específico a um botão.
        /// </summary>
        public static void ApplyIconToButton(Button button, string iconName)
        {
            if (button == null || string.IsNullOrEmpty(iconName)) return;

            var iconTexture = LoadIcon(iconName);
            if (iconTexture != null)
            {
                var existing = button.Q<VisualElement>("__icon");
                if (existing != null)
                {
                    ApplyBackground(existing, iconTexture);
                    return;
                }

                // Criar um VisualElement para o ícone
                var iconElement = new VisualElement { name = "__icon" };
                ApplyBackground(iconElement, iconTexture);
                iconElement.style.width = 16;
                iconElement.style.height = 16;
                iconElement.style.marginRight = 4;

                // Adicionar o ícone antes do texto
                button.Insert(0, iconElement);
            }
        }

        /// <summary>
        /// Define a imagem de fundo de um elemento com o ícone informado.
        /// </summary>
        public static void ApplyIconToElement(VisualElement element, string iconName)
        {
            if (element == null || string.IsNullOrEmpty(iconName))
            {
                return;
            }

            var iconTexture = LoadIcon(iconName);
            if (iconTexture == null)
            {
                return;
            }

            ApplyBackground(element, iconTexture);
        }

        private static void ApplyBackground(VisualElement element, Texture2D texture)
        {
            element.style.backgroundImage = texture;
            element.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);
            element.style.backgroundRepeat = new BackgroundRepeat(Repeat.NoRepeat, Repeat.NoRepeat);
            element.style.backgroundPositionX = new BackgroundPosition(BackgroundPositionKeyword.Center);
            element.style.backgroundPositionY = new BackgroundPosition(BackgroundPositionKeyword.Center);
        }

        /// <summary>
        /// Retorna a textura correspondente ao ícone informado.
        /// </summary>
        public static Texture2D GetIconTexture(string iconName)
        {
            return LoadIcon(iconName);
        }

        /// <summary>
        /// Carrega um ícone SVG e converte para Texture2D.
        /// </summary>
        private static Texture2D LoadIcon(string iconName)
        {
            if (_iconCache.TryGetValue(iconName, out var cachedIcon))
            {
                return cachedIcon;
            }

            // Tentar carregar o ícone como textura diretamente
            var texturePath = $"UI/Icons/{iconName}";
            var texture = Resources.Load<Texture2D>(texturePath);

            if (texture != null)
            {
                _iconCache[iconName] = texture;
                return texture;
            }

            // Tentar carregar como SVG e converter
            var svgPath = $"UI/Icons/{iconName}";
            var svgAsset = Resources.Load<TextAsset>(svgPath);

            if (svgAsset != null)
            {
                var convertedTexture = ConvertSvgToTexture(svgAsset.text, iconName);
                if (convertedTexture != null)
                {
                    _iconCache[iconName] = convertedTexture;
                    return convertedTexture;
                }
            }

            // Fallback: criar um ícone placeholder
            var placeholder = CreatePlaceholderIcon(iconName);
            _iconCache[iconName] = placeholder;
            return placeholder;
        }

        /// <summary>
        /// Converte SVG para Texture2D (implementação simples).
        /// </summary>
        private static Texture2D ConvertSvgToTexture(string svgContent, string iconName)
        {
            if (string.IsNullOrEmpty(svgContent))
            {
                return null;
            }

            var vectorMaterial = EnsureVectorMaterial();
            if (vectorMaterial == null)
            {
                return null;
            }

            try
            {
                using var reader = new StringReader(svgContent);
                var sceneInfo = SVGParser.ImportSVG(reader);

                var tessOptions = new VectorUtils.TessellationOptions
                {
                    StepDistance = 0.25f,
                    MaxCordDeviation = 0.5f,
                    MaxTanAngleDeviation = 0.1f,
                    SamplingStepSize = 0.01f
                };

                var geoms = VectorUtils.TessellateScene(sceneInfo.Scene, tessOptions, sceneInfo.NodeOpacity);
                if (geoms == null || geoms.Count == 0)
                {
                    return null;
                }

                var bounds = sceneInfo.SceneViewport;
                if (bounds.width <= 0f || bounds.height <= 0f)
                {
                    bounds = VectorUtils.SceneNodeBounds(sceneInfo.Scene);
                }

                if (bounds.width <= 0f || bounds.height <= 0f)
                {
                    bounds = new Rect(0, 0, IconResolution, IconResolution);
                }

                var sprite = VectorUtils.BuildSprite(geoms, bounds, 100f, VectorUtils.Alignment.Center, Vector2.zero, 128, false);
                if (sprite == null)
                {
                    return null;
                }

                var texture = VectorUtils.RenderSpriteToTexture2D(sprite, IconResolution, IconResolution, vectorMaterial, 4, true);
                texture.name = $"icon::{iconName}";
                texture.wrapMode = TextureWrapMode.Clamp;
                texture.filterMode = FilterMode.Bilinear;
                return texture;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"IconManager: falha ao converter SVG '{iconName}'. {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Cria um ícone placeholder baseado no nome.
        /// </summary>
        private static Texture2D CreatePlaceholderIcon(string iconName)
        {
            var texture = new Texture2D(16, 16, TextureFormat.RGBA32, false)
            {
                name = $"placeholder::{iconName}",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Point
            };
            var pixels = new Color[16 * 16];

            // Criar um padrão simples baseado no nome do ícone
            var color = GetIconColor(iconName);
            var pattern = GetIconPattern(iconName);
            
            for (int y = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    int index = y * 16 + x;
                    if (pattern[y, x])
                    {
                        pixels[index] = color;
                    }
                    else
                    {
                        pixels[index] = new Color(0, 0, 0, 0); // Transparent
                    }
                }
            }
            
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        /// <summary>
        /// Retorna uma cor baseada no tipo de ícone.
        /// </summary>
        private static Color GetIconColor(string iconName)
        {
            return iconName switch
            {
                "contract" => new Color(0.2f, 0.6f, 1f), // Azul
                "pause-button" => new Color(1f, 0.3f, 0.3f), // Vermelho
                "play-button" => new Color(0.3f, 0.8f, 0.3f), // Verde
                "fast-forward-button" => new Color(1f, 0.8f, 0.2f), // Amarelo
                "save" => new Color(0.2f, 0.8f, 0.8f), // Ciano
                "load" => new Color(0.8f, 0.2f, 0.8f), // Magenta
                "histogram" => new Color(0.9f, 0.9f, 0.9f), // Branco
                "build" => new Color(0.8f, 0.4f, 0.2f), // Marrom
                "large-paint-brush" => new Color(1f, 0.5f, 0.8f), // Rosa
                "mona-lisa" => new Color(0.5f, 0.2f, 0.8f), // Roxo
                _ => new Color(0.5f, 0.5f, 0.5f) // Cinza
            };
        }

        /// <summary>
        /// Retorna um padrão baseado no tipo de ícone.
        /// </summary>
        private static bool[,] GetIconPattern(string iconName)
        {
            var pattern = new bool[16, 16];
            
            switch (iconName)
            {
                case "contract":
                    // Retângulo simples
                    for (int y = 4; y < 12; y++)
                        for (int x = 4; x < 12; x++)
                            pattern[y, x] = true;
                    break;
                    
                case "pause-button":
                    // Dois quadrados verticais
                    for (int y = 4; y < 12; y++)
                    {
                        pattern[y, 5] = true;
                        pattern[y, 6] = true;
                        pattern[y, 9] = true;
                        pattern[y, 10] = true;
                    }
                    break;
                    
                case "play-button":
                    // Triângulo apontando para direita
                    for (int y = 4; y < 12; y++)
                    {
                        for (int x = 4 + (y - 4); x < 12 - (y - 4); x++)
                            pattern[y, x] = true;
                    }
                    break;
                    
                case "fast-forward-button":
                    // Dois triângulos
                    for (int y = 4; y < 12; y++)
                    {
                        for (int x = 4 + (y - 4) / 2; x < 8 - (y - 4) / 2; x++)
                            pattern[y, x] = true;
                        for (int x = 8 + (y - 4) / 2; x < 12 - (y - 4) / 2; x++)
                            pattern[y, x] = true;
                    }
                    break;
                    
                case "save":
                    // Disquete
                    for (int y = 4; y < 12; y++)
                    {
                        for (int x = 4; x < 12; x++)
                            pattern[y, x] = true;
                    }
                    // Label
                    for (int y = 6; y < 10; y++)
                    {
                        pattern[y, 6] = false;
                        pattern[y, 7] = false;
                        pattern[y, 8] = false;
                        pattern[y, 9] = false;
                    }
                    break;
                    
                case "load":
                    // Seta circular
                    for (int y = 4; y < 12; y++)
                    {
                        for (int x = 4; x < 12; x++)
                        {
                            int dx = x - 8;
                            int dy = y - 8;
                            if (dx * dx + dy * dy <= 16)
                                pattern[y, x] = true;
                        }
                    }
                    break;
                    
                case "histogram":
                    // Barras
                    for (int y = 4; y < 12; y++)
                    {
                        pattern[y, 4] = true;
                        pattern[y, 6] = true;
                        pattern[y, 8] = true;
                        pattern[y, 10] = true;
                    }
                    break;
                    
                case "build":
                    // Martelo
                    for (int y = 6; y < 10; y++)
                    {
                        for (int x = 4; x < 12; x++)
                            pattern[y, x] = true;
                    }
                    break;
                    
                case "large-paint-brush":
                    // Pincel
                    for (int y = 4; y < 12; y++)
                    {
                        pattern[y, 8] = true;
                        pattern[y, 9] = true;
                    }
                    break;
                    
                case "mona-lisa":
                    // Círculo
                    for (int y = 4; y < 12; y++)
                    {
                        for (int x = 4; x < 12; x++)
                        {
                            int dx = x - 8;
                            int dy = y - 8;
                            if (dx * dx + dy * dy <= 16)
                                pattern[y, x] = true;
                        }
                    }
                    break;
                    
                default:
                    // Quadrado simples
                    for (int y = 4; y < 12; y++)
                        for (int x = 4; x < 12; x++)
                            pattern[y, x] = true;
                    break;
            }
            
            return pattern;
        }

        /// <summary>
        /// Limpa o cache de ícones.
        /// </summary>
        public static void ClearCache()
        {
            foreach (var texture in _iconCache.Values)
            {
                if (texture != null)
                {
                    Object.DestroyImmediate(texture);
                }
            }
            _iconCache.Clear();

            if (_vectorMaterial != null)
            {
                Object.DestroyImmediate(_vectorMaterial);
                _vectorMaterial = null;
            }

            _vectorShaderUnavailable = false;
        }

        private static Material EnsureVectorMaterial()
        {
            if (_vectorMaterial != null)
            {
                return _vectorMaterial;
            }

            if (_vectorShaderUnavailable)
            {
                return null;
            }

            var shader = Shader.Find("VectorGraphics/VectorSprite");
            if (shader == null)
            {
                Debug.LogWarning("IconManager: shader 'VectorGraphics/VectorSprite' não encontrado. Ícones SVG usarão placeholder.");
                _vectorShaderUnavailable = true;
                return null;
            }

            _vectorMaterial = new Material(shader)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            return _vectorMaterial;
        }
    }
}
