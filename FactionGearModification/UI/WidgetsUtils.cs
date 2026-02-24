using UnityEngine;
using Verse;
using HarmonyLib;
using System;
using System.Reflection;

namespace FactionGearModification.UI
{
    public static class WidgetsUtils
    {
        private static MethodInfo floatRangeMethod;
        private static object[] floatRangeArgs;

        public static void SplitRow2(Rect row, float gap, float leftFraction, out Rect left, out Rect right)
        {
            float x0 = Mathf.Floor(row.x);
            float xMax = Mathf.Floor(row.xMax);
            float y0 = Mathf.Floor(row.y);
            float yMax = Mathf.Floor(row.yMax);
            float w = Mathf.Max(0f, xMax - x0);
            float h = Mathf.Max(0f, yMax - y0);
            float avail = Mathf.Max(0f, w - gap);

            float leftW = Mathf.Floor(avail * leftFraction);
            float rightW = avail - leftW;

            left = new Rect(x0, y0, leftW, h);
            right = new Rect(x0 + leftW + gap, y0, rightW, h);
        }

        public static void SplitRow3(Rect row, float gap, float firstFraction, float secondFraction, out Rect first, out Rect second, out Rect third)
        {
            float x0 = Mathf.Floor(row.x);
            float xMax = Mathf.Floor(row.xMax);
            float y0 = Mathf.Floor(row.y);
            float yMax = Mathf.Floor(row.yMax);
            float w = Mathf.Max(0f, xMax - x0);
            float h = Mathf.Max(0f, yMax - y0);
            float avail = Mathf.Max(0f, w - gap * 2f);

            float w0 = Mathf.Floor(avail * firstFraction);
            float w1 = Mathf.Floor(avail * secondFraction);
            float w2 = avail - w0 - w1;

            first = new Rect(x0, y0, w0, h);
            second = new Rect(x0 + w0 + gap, y0, w1, h);
            third = new Rect(x0 + w0 + gap + w1 + gap, y0, w2, h);
        }

        public static void DrawTextureFitted(Rect outerRect, Texture tex, float scale)
        {
            if (tex == null)
            {
                return;
            }

            Rect rect = new Rect(0f, 0f, (float)tex.width, (float)tex.height);
            float num = rect.width / rect.height;
            float num2 = outerRect.width / outerRect.height;
            if (num > num2)
            {
                float height = outerRect.width / num;
                rect = new Rect(0f, (outerRect.height - height) / 2f, outerRect.width, height);
            }
            else
            {
                float width = outerRect.height * num;
                rect = new Rect((outerRect.width - width) / 2f, 0f, width, outerRect.height);
            }
            rect.width *= scale;
            rect.height *= scale;
            rect.x += outerRect.x + (outerRect.width - rect.width) / 2f;
            rect.y += outerRect.y + (outerRect.height - rect.height) / 2f;
            
            GUI.DrawTexture(rect, tex);
        }

        public static void Label(Listing_Standard listing, string label, float maxHeight = -1f, string tooltip = null)
        {
            Rect rect = listing.GetRect(Text.CalcHeight(label, listing.ColumnWidth));
            Widgets.Label(rect, label);
            if (tooltip != null)
            {
                TooltipHandler.TipRegion(rect, tooltip);
            }
            listing.Gap(2f);
        }

        public static void FloatRange(Rect rect, int id, ref FloatRange range, float min = 0f, float max = 1f, string label = null, ToStringStyle style = ToStringStyle.FloatTwo)
        {
            try
            {
                if (floatRangeMethod == null)
                {
                    // Aggressive Search: Find ALL methods named "FloatRange" in Widgets
                    var allMethods = AccessTools.GetDeclaredMethods(typeof(Widgets));
                    MethodInfo bestMatch = null;
                    
                    foreach (var m in allMethods)
                    {
                        if (m.Name == "FloatRange")
                        {
                            var p = m.GetParameters();
                            // Look for the signature that takes a ref FloatRange
                            if (p.Length >= 3 && p[2].ParameterType == typeof(FloatRange).MakeByRefType())
                            {
                                bestMatch = m;
                                break; // Found a good candidate
                            }
                        }
                    }

                    floatRangeMethod = bestMatch;

                    if (floatRangeMethod == null)
                    {
                        Log.WarningOnce("[FactionGearModification] Widgets.FloatRange method not found via reflection. Using fallback UI.", 9812373);
                    }
                }

                if (floatRangeMethod != null)
                {
                    var methodParams = floatRangeMethod.GetParameters();
                    object[] args = new object[methodParams.Length];
                    
                    // Map standard parameters
                    if (args.Length > 0) args[0] = rect;
                    if (args.Length > 1) args[1] = id;
                    if (args.Length > 2) args[2] = range;
                    if (args.Length > 3) args[3] = min;
                    if (args.Length > 4) args[4] = max;
                    if (args.Length > 5) args[5] = label;
                    if (args.Length > 6) args[6] = style;

                    // Fill defaults
                    for (int i = 7; i < args.Length; i++)
                    {
                        var param = methodParams[i];
                        if (param.HasDefaultValue) args[i] = param.DefaultValue;
                        else if (param.ParameterType.IsValueType) args[i] = Activator.CreateInstance(param.ParameterType);
                        else args[i] = null;
                    }

                    floatRangeMethod.Invoke(null, args);
                    
                    if (args.Length > 2)
                    {
                        range = (FloatRange)args[2];
                    }
                    return; // Success
                }
            }
            catch (Exception ex)
            {
                Log.WarningOnce($"[FactionGearModification] Reflection failed for FloatRange: {ex.Message}. Using fallback UI.", 9812374);
            }

            // Fallback UI: Two Horizontal Sliders
            // Split rect into two halves
            float mid = rect.width / 2f;
            Rect leftRect = new Rect(rect.x, rect.y, mid - 2f, rect.height);
            Rect rightRect = new Rect(rect.x + mid + 2f, rect.y, mid - 2f, rect.height);

            // Min Slider (controlled by min -> current max)
            float newMin = Widgets.HorizontalSlider(leftRect, range.min, min, range.max, false, null, "Min");
            
            // Max Slider (controlled by current min -> max)
            float newMax = Widgets.HorizontalSlider(rightRect, range.max, range.min, max, false, null, "Max");

            if (newMin != range.min || newMax != range.max)
            {
                range = new FloatRange(newMin, newMax);
            }
        }

        private static MethodInfo intRangeMethod;

        public static void IntRange(Rect rect, int id, ref IntRange range, int min = 0, int max = 100, string label = null, int minWidth = 0)
        {
            try
            {
                if (intRangeMethod == null)
                {
                    var allMethods = AccessTools.GetDeclaredMethods(typeof(Widgets));
                    MethodInfo bestMatch = null;
                    
                    foreach (var m in allMethods)
                    {
                        if (m.Name == "IntRange")
                        {
                            var p = m.GetParameters();
                            if (p.Length >= 3 && p[2].ParameterType == typeof(IntRange).MakeByRefType())
                            {
                                bestMatch = m;
                                break;
                            }
                        }
                    }

                    intRangeMethod = bestMatch;

                    if (intRangeMethod == null)
                    {
                        Log.WarningOnce("[FactionGearModification] Widgets.IntRange method not found via reflection. Using fallback UI.", 9812375);
                    }
                }

                if (intRangeMethod != null)
                {
                    var methodParams = intRangeMethod.GetParameters();
                    object[] args = new object[methodParams.Length];
                    
                    if (args.Length > 0) args[0] = rect;
                    if (args.Length > 1) args[1] = id;
                    if (args.Length > 2) args[2] = range;
                    if (args.Length > 3) args[3] = min;
                    if (args.Length > 4) args[4] = max;
                    if (args.Length > 5) args[5] = label;
                    if (args.Length > 6) args[6] = minWidth;

                    for (int i = 7; i < args.Length; i++)
                    {
                        var param = methodParams[i];
                        if (param.HasDefaultValue) args[i] = param.DefaultValue;
                        else if (param.ParameterType.IsValueType) args[i] = Activator.CreateInstance(param.ParameterType);
                        else args[i] = null;
                    }

                    intRangeMethod.Invoke(null, args);
                    
                    if (args.Length > 2)
                    {
                        range = (IntRange)args[2];
                    }
                    return;
                }
            }
            catch (Exception ex)
            {
                Log.WarningOnce($"[FactionGearModification] Reflection failed for IntRange: {ex.Message}. Using fallback UI.", 9812376);
            }

            // Fallback UI
            float mid = rect.width / 2f;
            Rect leftRect = new Rect(rect.x, rect.y, mid - 2f, rect.height);
            Rect rightRect = new Rect(rect.x + mid + 2f, rect.y, mid - 2f, rect.height);

            float newMin = Widgets.HorizontalSlider(leftRect, (float)range.min, (float)min, (float)range.max, false, null, "Min");
            float newMax = Widgets.HorizontalSlider(rightRect, (float)range.max, (float)range.min, (float)max, false, null, "Max");

            int finalMin = Mathf.RoundToInt(newMin);
            int finalMax = Mathf.RoundToInt(newMax);

            if (finalMin != range.min || finalMax != range.max)
            {
                range = new IntRange(finalMin, finalMax);
            }
        }

        private static MethodInfo drawBoxMethod;

        public static void DrawBox(Rect rect, int thickness = 1, Texture2D customEdge = null)
        {
            try
            {
                if (drawBoxMethod == null)
                {
                    // Find DrawBox. 1.5 has 3 args, 1.4 has 2 args.
                    var methods = AccessTools.GetDeclaredMethods(typeof(Widgets));
                    foreach (var m in methods)
                    {
                        if (m.Name == "DrawBox")
                        {
                            var p = m.GetParameters();
                            if (p.Length >= 1 && p[0].ParameterType == typeof(Rect))
                            {
                                // Prefer the one with more arguments (1.5) if available, or just take the first one found
                                // Actually, we should check if it matches what we want to call.
                                // If we want to call with 3 args, we need 3 args.
                                // If we only find 2 args, we call with 2.
                                drawBoxMethod = m;
                                if (p.Length == 3) break; 
                            }
                        }
                    }
                }

                if (drawBoxMethod != null)
                {
                    var p = drawBoxMethod.GetParameters();
                    object[] args = new object[p.Length];
                    args[0] = rect;
                    if (p.Length > 1) args[1] = thickness;
                    if (p.Length > 2) args[2] = customEdge;
                    
                    drawBoxMethod.Invoke(null, args);
                    return;
                }
            }
            catch (Exception ex)
            {
                Log.WarningOnce($"[FactionGearModification] Reflection failed for DrawBox: {ex.Message}. Using fallback.", 9812377);
            }

            // Fallback: Manual DrawBox
            Vector2 topLeft = new Vector2(rect.x, rect.y);
            Vector2 topRight = new Vector2(rect.x + rect.width - 1f, rect.y);
            Vector2 bottomLeft = new Vector2(rect.x, rect.y + rect.height - 1f);
            Vector2 bottomRight = new Vector2(rect.x + rect.width - 1f, rect.y + rect.height - 1f);

            Widgets.DrawLineHorizontal(topLeft.x, topLeft.y, rect.width);
            Widgets.DrawLineHorizontal(bottomLeft.x, bottomLeft.y, rect.width);
            Widgets.DrawLineVertical(topLeft.x, topLeft.y, rect.height);
            Widgets.DrawLineVertical(topRight.x, topRight.y, rect.height);
        }

        private static MethodInfo drawMenuSectionMethod;

        public static void DrawMenuSection(Rect rect)
        {
            try
            {
                if (drawMenuSectionMethod == null)
                {
                    drawMenuSectionMethod = AccessTools.Method(typeof(Widgets), "DrawMenuSection");
                }

                if (drawMenuSectionMethod != null)
                {
                    drawMenuSectionMethod.Invoke(null, new object[] { rect });
                    return;
                }
            }
            catch (Exception ex)
            {
                Log.WarningOnce($"[FactionGearModification] Reflection failed for DrawMenuSection: {ex.Message}. Using fallback.", 9812378);
            }

            // Fallback
            Widgets.DrawBoxSolid(rect, new Color(0.17f, 0.17f, 0.17f)); // Dark background
            DrawBox(rect); // Border
        }

        private static MethodInfo drawWindowBackgroundMethod;

        public static void DrawWindowBackground(Rect rect)
        {
            try
            {
                if (drawWindowBackgroundMethod == null)
                {
                    drawWindowBackgroundMethod = AccessTools.Method(typeof(Widgets), "DrawWindowBackground", new Type[] { typeof(Rect) });
                }

                if (drawWindowBackgroundMethod != null)
                {
                    drawWindowBackgroundMethod.Invoke(null, new object[] { rect });
                    return;
                }
            }
            catch (Exception ex)
            {
                Log.WarningOnce($"[FactionGearModification] Reflection failed for DrawWindowBackground: {ex.Message}", 9812380);
            }
            
            // Fallback
            Widgets.DrawBoxSolid(rect, new Color(0.1f, 0.1f, 0.1f, 0.8f));
            DrawBox(rect);
        }

        private static MethodInfo portraitsCacheGetMethod;
        private static Type portraitsCacheType;

        private static Type GetPortraitsCacheType()
        {
            if (portraitsCacheType == null)
            {
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    try
                    {
                        portraitsCacheType = assembly.GetType("PortraitsCache");
                        if (portraitsCacheType != null) break;
                        portraitsCacheType = assembly.GetType("RimWorld.PortraitsCache");
                        if (portraitsCacheType != null) break;
                    }
                    catch
                    {
                    }
                }
            }
            return portraitsCacheType;
        }

        public static RenderTexture GetPortrait(Pawn pawn, Vector2 size, Rot4 rotation, Vector3 cameraOffset = default(Vector3), float cameraZoom = 1f)
        {
            try
            {
                var portraitsCache = GetPortraitsCacheType();
                if (portraitsCache == null)
                {
                    Log.WarningOnce("[FactionGearModification] PortraitsCache type not found", 9812380);
                    return null;
                }

                if (portraitsCacheGetMethod == null)
                {
                    var methods = AccessTools.GetDeclaredMethods(portraitsCache);
                    foreach (var m in methods)
                    {
                        if (m.Name == "Get")
                        {
                            var p = m.GetParameters();
                            // First param is Pawn, second is Vector2
                            if (p.Length >= 2 && p[0].ParameterType == typeof(Pawn) && p[1].ParameterType == typeof(Vector2))
                            {
                                portraitsCacheGetMethod = m;
                                break;
                            }
                        }
                    }
                }

                if (portraitsCacheGetMethod != null)
                {
                    var p = portraitsCacheGetMethod.GetParameters();
                    object[] args = new object[p.Length];
                    args[0] = pawn;
                    args[1] = size;
                    if (p.Length > 2) args[2] = rotation;
                    if (p.Length > 3) args[3] = cameraOffset;
                    if (p.Length > 4) args[4] = cameraZoom;
                    
                    // Fill remaining defaults
                    for (int i = 5; i < args.Length; i++)
                    {
                        var param = p[i];
                        if (param.HasDefaultValue) args[i] = param.DefaultValue;
                        else if (param.ParameterType.IsValueType) args[i] = Activator.CreateInstance(param.ParameterType);
                        else args[i] = null;
                    }

                    return (RenderTexture)portraitsCacheGetMethod.Invoke(null, args);
                }
            }
            catch (Exception ex)
            {
                Log.WarningOnce($"[FactionGearModification] Reflection failed for PortraitsCache.Get: {ex.Message}", 9812379);
            }
            return null;
        }

        private static MethodInfo portraitsCacheSetDirtyMethod;

        public static void SetPortraitDirty(Pawn pawn)
        {
            try
            {
                var portraitsCache = GetPortraitsCacheType();
                if (portraitsCache == null) return;

                if (portraitsCacheSetDirtyMethod == null)
                {
                    portraitsCacheSetDirtyMethod = AccessTools.Method(portraitsCache, "SetDirty", new Type[] { typeof(Pawn) });
                }
                
                if (portraitsCacheSetDirtyMethod != null)
                {
                    portraitsCacheSetDirtyMethod.Invoke(null, new object[] { pawn });
                }
            }
            catch
            {
                // Ignore
            }
        }
    }
}
