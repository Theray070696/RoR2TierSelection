using System;
using System.Collections.Generic;
using System.Linq;
using RoR2;
using UnityEngine;

namespace Theray070696
{
    public class ColorTransformer
    {
        private const int MaskAlphaThreshold = 100;
        
        /// <summary>
        /// Estimated color to use as replacement.
        /// </summary>
        private static readonly Dictionary<ItemTier, Color32> TierColors = new Dictionary<ItemTier, Color32>()
        {
            {ItemTier.Tier1, new Color32(192,204,206,255)},
            {ItemTier.Tier2, new Color32(107,190,66,255)},
            {ItemTier.Tier3, new Color32(244,91,65,255)},
            {ItemTier.Boss, new Color32(206,218,35,255)},
            {ItemTier.Lunar, new Color32(66,223,247,255)},
        };
        
        /// <summary>
        /// Color range for every tier for use in identifying tier outline groups.
        /// </summary>
        private static readonly Dictionary<ItemTier, (Color32 min, Color32 max)> TieredColorRange = new Dictionary<ItemTier, (Color32 min, Color32 max)>()
        {
            {ItemTier.Tier1, (new Color32(145, 169, 173, 0), new Color32(255, 255, 255, 0))},
            {ItemTier.Tier2, (new Color32(90, 160, 0, 0), new Color32(150, 255, 90, 0))},
            {ItemTier.Tier3, (new Color32(239, 70, 20, 0), new Color32(255, 100, 100, 0))},    
            {ItemTier.Lunar, (new Color32(30, 169, 173, 0), new Color32(90, 255, 255, 0))},
            {ItemTier.Boss, (new Color32(120, 150, 0, 0), new Color32(255, 255, 60, 0))},
        };
        
        /// <summary>
        /// Loads game texture from itemDef.pickupIconPath and returns one with the correct color for the new rarity.
        /// </summary>
        /// <param name="itemDef"></param>
        /// <param name="newTier"></param>
        /// <returns></returns>
        public static Texture GenerateTexture(ItemDef itemDef, ItemTier newTier)
        {
            // Load texture.
            var img = Resources.Load<Texture2D>(itemDef.pickupIconPath);
            var imgPixels = GetPixelsFromNonReadableTexture(img);
            
            // Create a binary mask based on the alpha channel. This gives clear boundaries between the tier outline and the
            // item.
            var mask = imgPixels.Select(c => c.a < MaskAlphaThreshold ? 0 : 1).ToArray();
            
            // Find the groups of pixels using a flood fill algorithm. The key is the group id.
            var groups = GetGroupsInMask(mask, img.width);
            
            // Calculate the average color of the pixels in every group. The key is the group id.
            var averageColors = groups.Select(kv => new KeyValuePair<int, Color32>(kv.Key, AverageColor(imgPixels, kv.Value))).ToDictionary(kv => kv.Key, kv => kv.Value);
            
            // Find all pixels in the texture that correspond to a group with the average color close to the tier outline.
            var recolorGroups = groups.Where(kv => CheckColor(itemDef.tier, averageColors[kv.Key])).SelectMany(kv => kv.Value);
            
            // Update pixel colors to the new tier.
            foreach (var color in recolorGroups)
            {
                imgPixels[color.Index] = TierColors[newTier];
            }

            // Load changed pixels into a texture for returning.
            var tex = new Texture2D(img.width, img.height, TextureFormat.RGBA32, false);
            tex.SetPixels32(imgPixels);
            tex.Apply(true, true); // Make no longer readable to save memory.
            return tex;
        }
        
        private static bool CheckColor(ItemTier tier, Color32 averageColor)
        {
            var (min, max) = TieredColorRange[tier];
            return
                  min.r < averageColor.r && averageColor.r < max.r
               && min.g < averageColor.g && averageColor.g < max.g
               && min.b < averageColor.b && averageColor.b < max.b;
        }

        /// <summary>
        /// Averages the color using the algorithm described here: https://sighack.com/post/averaging-rgb-colors-the-right-way
        /// </summary>
        /// <param name="imgPixels"></param>
        /// <param name="nodes"></param>
        /// <returns></returns>
        private static Color32 AverageColor(Color32[] imgPixels, List<Node> nodes)
        {
            var r = 0;
            var g = 0;
            var b = 0;

            foreach (var color in nodes.Select(node => imgPixels[node.Index]))
            {
                r += color.r * color.r;
                g += color.g * color.g;
                b += color.b * color.b;
            }

            r /= nodes.Count;
            g /= nodes.Count;
            b /= nodes.Count;

            r = (int) Mathf.Sqrt(r);
            g = (int) Mathf.Sqrt(g);
            b = (int) Mathf.Sqrt(b);
            
            return new Color32((byte) r, (byte) g, (byte) b, 255);
        }

        /// <summary>
        /// Searches binary mask for contiguous groups.
        /// </summary>
        /// <param name="mask"></param>
        /// <param name="width"></param>
        /// <returns></returns>
        private static Dictionary<int, List<Node>> GetGroupsInMask(int[] mask, int width)
        {
            var result = new Dictionary<int, List<Node>>();
            var group = 2;
            for (var i = 0; i < mask.Length; i++)
            {
                if (mask[i] != 1) continue;
                result[group] = FloodFillPixel(mask, new Node(i, width, mask.Length / width), 1, group);
                group++;
            }
            
            return result;
        }

        /// <summary>
        /// Finds all pixels in a group using a scan line algorithm.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="node"></param>
        /// <param name="target"></param>
        /// <param name="replacement"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        private static List<Node> FloodFillPixel<T>(T[] input, Node node, T target, T replacement)
        {
            var result = new List<Node>();
            var nextScanLine = new HashSet<Node>();
            var reverseScan = new HashSet<Node>();
            var curr = node;
            var nodesChecked = new bool[2];
            var leftNodeChecked = false;
            var forward = true;

            while (curr.Index != -1)
            {
                // Replace current node value and add it to the changed node list
                result.Add(curr);
                input[curr.Index] = replacement;
                
                // Search for new start points above and below. Every time there is a gap in valid pixels a new start
                // point will be created.
                var nodesToCheck = new[] {curr.Top, curr.Bottom};
                for (var i = 0; i < nodesToCheck.Length; i++)
                {
                    var checkingNode = nodesToCheck[i];
                    if (!checkingNode.IsValid) continue;
                    
                    var checkingNodeIsTarget = input[checkingNode.Index].Equals(target);
                    if (!nodesChecked[i] && checkingNodeIsTarget)
                    {
                        nodesChecked[i] = true;
                        nextScanLine.Add(checkingNode);
                    } else if (nodesChecked[i] && !checkingNodeIsTarget)
                    {
                        nodesChecked[i] = false;
                    }
                }
                
                // Check for nodes to the left. If there are, add a custom start point that reverses direction just for
                // the line.
                if (!leftNodeChecked)
                {
                    var leftNode = curr.Left;
                    if (input[leftNode.Index].Equals(target))
                    {
                        reverseScan.Add(leftNode);
                    }

                    leftNodeChecked = true;
                }
                
                // Set next node
                var nextNode = forward ? curr.Right : curr.Left;
                if (input[nextNode.Index].Equals(target))
                {
                    curr = nextNode;
                }
                else if (reverseScan.Count > 0)
                {
                    curr = reverseScan.First();
                    reverseScan.Remove(curr);
                    nodesChecked = new bool[2];
                    forward = false;
                }
                else if (nextScanLine.Count > 0)
                {
                    curr = nextScanLine.First();
                    nextScanLine.Remove(curr);
                    nodesChecked = new bool[2];
                    leftNodeChecked = false;
                    forward = true;
                }
                else
                {
                    curr = new Node(-1, node.Width, node.Height);
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// Reads the texture even if the texture is marked unreadable.
        /// </summary>
        /// <param name="img"></param>
        /// <returns></returns>
        private static Color32[] GetPixelsFromNonReadableTexture(Texture img)
        {
            var rt = RenderTexture.GetTemporary(img.width, img.height);
            Graphics.Blit(img, rt);
            var previousRt = RenderTexture.active;
            RenderTexture.active = rt;
            var img2 = new Texture2D(img.width, img.height);
            img2.ReadPixels(new Rect(0, 0, img.width, img.height), 0, 0);
            img2.Apply();
            RenderTexture.active = previousRt;
            RenderTexture.ReleaseTemporary(rt);
            return img2.GetPixels32();
        }
    }
    
    /// <summary>
    /// Struct for handling the math for 1D pixels.
    /// </summary>
    internal readonly struct Node
    {
        public readonly int Index;
        public readonly int Width;
        public readonly int Height;
        public readonly int X;
        public readonly int Y;
        public readonly bool IsValid;
        
        public Node(int x, int y, int width, int height) : this(y * width + x, width, height) {}

        public Node(int index, int width, int height)
        {
            Index = index;
            Width = width;
            Height = height;
            Y = index / width;
            X = index - Y * width;
            IsValid = Index >= 0 && Index < width * height;
        }
        
        public Node Top => new Node(X, Y - 1, Width, Height);
        public Node Bottom => new Node(X, Y + 1, Width, Height);
        public Node Left => new Node(X - 1, Y, Width, Height);
        public Node Right => new Node(X + 1, Y, Width, Height);
    }
}