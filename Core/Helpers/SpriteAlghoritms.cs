using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace Moth.Core.Helpers
{
    internal class SpriteAlghoritms
    {
        // First position rectangles more than half as wide as the bin.
        // Then position the remaining rectangles in two columns.
        private void AlgTwoColumns(int bin_width, SpriteRule[] rects)
        {
            // Separate the wide and narrow SpriteRules.
            ArrayList wide_list = new ArrayList();
            ArrayList narrow_list = new ArrayList();
            for (int i = 0; i <= rects.Length - 1; i++)
            {
                if (rects[i].Width > bin_width / 2)
                {
                    wide_list.Add(rects[i]);
                }
                else
                {
                    narrow_list.Add(rects[i]);
                }
            }

            // Sort the wide SpriteRules by width.
            SpriteRule[] wide_array = (SpriteRule[])wide_list.ToArray(typeof(SpriteRule));
            Array.Sort(wide_array, new SpriteWidthComparer());

            // Sort the narrow SpriteRules by height.
            SpriteRule[] narrow_array = (SpriteRule[])narrow_list.ToArray(typeof(SpriteRule));
            Array.Sort(narrow_array, new SpriteHeightComparer());

            // Arrange the wide SpriteRules.
            int x = 0;
            int y = 0;
            for (int i = 0; i <= wide_array.Length - 1; i++)
            {
                wide_array[i].X = x;
                wide_array[i].Y = y;
                y += wide_array[i].Height;
            }

            // Arrange the narrow SpriteRules.
            if (narrow_array.Length > 0)
            {
                // Make two columns.
                System.Collections.Generic.List<SpriteRule>[] cols =
                    new System.Collections.Generic.List<SpriteRule>[1];
                int[] col_wids = { bin_width / 2, bin_width - bin_width / 2 };
                int[] col_ys = { y, y };
                int[] col_xs = { 0, col_wids[0] };

                // Track the index of the next SpriteRule.
                int next_rect = 0;

                // Process until all SpriteRules are positioned.
                while (next_rect < narrow_array.Length)
                {
                    // See which column is shorter.
                    int col;
                    if (col_ys[0] <= col_ys[1])
                        col = 0;
                    else
                        col = 1;

                    // Figure out where this row will stop.
                    int next_y = col_ys[col] + narrow_array[next_rect].Height;

                    // Keep track of the width used.
                    int row_height = 0;

                    // Add SpriteRules to the selected column until no more fit.
                    while (true)
                    {
                        // If the next SpriteRule doesn//t fit,
                        // stop filling this column slice.
                        if (row_height + narrow_array[next_rect].Width > col_wids[col]) break;

                        // Position the SpriteRule.
                        narrow_array[next_rect].X = col_xs[col] + row_height;
                        row_height += narrow_array[next_rect].Width;
                        narrow_array[next_rect].Y = col_ys[col];

                        // If we ran out of SpriteRules, we//re done.
                        next_rect += 1;
                        if (next_rect >= narrow_array.Length) break;
                    }

                    // Update the column//s largest Y value.
                    col_ys[col] = next_y;
                }

                // Record the largest Y value.
                y = Math.Max(col_ys[0], col_ys[1]);
            }

            // Combine the results into the rect array.
            Array.Copy(wide_array, rects, wide_array.Length);
            Array.Copy(narrow_array, 0, rects, wide_array.Length, narrow_array.Length);
        }

        // First position SpriteRules more than half as wide as the bin.
        // Then position the remaining SpriteRules in two columns.
        public void AlgOneColumn(int bin_width, SpriteRule[] rects)
        {
            // Find the wide SpriteRules.
            ArrayList wide_col = new ArrayList();
            ArrayList narrow_col = new ArrayList();
            for (int i = 0; i <= rects.Length - 1; i++)
            {
                if (rects[i].Width > bin_width / 2)
                    wide_col.Add(rects[i]);
                else
                    narrow_col.Add(rects[i]);
            }

            // Sort the wide SpriteRules by width.
            SpriteRule[] wide_array = new SpriteRule[wide_col.Count];
            for (int i = 0; i <= wide_col.Count - 1; i++)
            {
                wide_array[i] = (SpriteRule)wide_col[i];
            }
            Array.Sort(wide_array, new SpriteWidthComparer());

            // Sort the narrow SpriteRules by height.
            SpriteRule[] narrow_array = new SpriteRule[narrow_col.Count];
            for (int i = 0; i <= narrow_col.Count - 1; i++)
            {
                narrow_array[i] = (SpriteRule)narrow_col[i];
            }
            Array.Sort(narrow_array, new SpriteHeightComparer());

            // Arrange the wide SpriteRules.
            int x = 0;
            int y = 0;
            for (int i = 0; i <= wide_array.Length - 1; i++)
            {
                wide_array[i].X = x;
                wide_array[i].Y = y;
                y += wide_array[i].Height;
            }

            // Arrange the narrow SpriteRules.
            if (narrow_array.Length > 0)
            {
                int row_height = narrow_array[0].Height;
                for (int i = 0; i <= narrow_array.Length - 1; i++)
                {
                    if (x + narrow_array[i].Width <= bin_width)
                    {
                        // Add to this row.
                        narrow_array[i].X = x;
                        narrow_array[i].Y = y;
                    }
                    else
                    {
                        // Start a new row.
                        x = 0;
                        y += row_height;
                        row_height = narrow_array[i].Height;

                        narrow_array[i].X = x;
                        narrow_array[i].Y = y;
                    }
                    x += narrow_array[i].Width;
                }
            }

            // Combine the results into the rect array.
            Array.Copy(wide_array, rects, wide_array.Length);
            Array.Copy(narrow_array, 0, rects, wide_array.Length, narrow_array.Length);
        }

        // Sort SpriteRules by height.
        // Fill in by rows in a single column.
        public void AlgSortByHeight(int bin_width, SpriteRule[] rects)
        {
            // Sort by height.
            Array.Sort(rects, new SpriteHeightComparer());

            // Fill in one column.
            SubAlgFillOneColumn(bin_width, rects);
        }

        // Sort SpriteRules by width.
        // Fill in by rows in a single column.
        public void AlgSortByWidth(int bin_width, SpriteRule[] rects)
        {
            // Sort by height.
            Array.Sort(rects, new SpriteWidthComparer());

            // Fill in one column.
            SubAlgFillOneColumn(bin_width, rects);
        }

        // Sort SpriteRules by area.
        // Fill in by rows in a single column.
        public void AlgSortByArea(int bin_width, SpriteRule[] rects)
        {
            // Sort by height.
            Array.Sort(rects, new SpriteAreaComparer());

            // Fill in one column.
            SubAlgFillOneColumn(bin_width, rects);
        }

        // Sort SpriteRules by squareness.
        // Fill in by rows in a single column.
        public void AlgSortBySquareness(int bin_width, SpriteRule[] rects)
        {
            // Sort by height.
            Array.Sort(rects, new SpriteSquarenessComparer());

            // Fill in one column.
            SubAlgFillOneColumn(bin_width, rects);
        }

        // Fill in by rows in a single column.
        public void SubAlgFillOneColumn(int bin_width, SpriteRule[] rects)
        {
            // Make lists of positioned and not positioned SpriteRules.
            List<SpriteRule> not_positioned = new List<SpriteRule>();
            List<SpriteRule> positioned = new List<SpriteRule>();
            for (int i = 0; i <= rects.Length - 1; i++)
                not_positioned.Add(rects[i]);

            // Arrange the SpriteRules.
            int x = 0;
            int y = 0;
            int row_hgt = 0;
            while (not_positioned.Count > 0)
            {
                // Find the next SpriteRule that will fit on this row.
                int next_rect = -1;
                for (int i = 0; i <= not_positioned.Count - 1; i++)
                {
                    if (x + not_positioned[i].Width <= bin_width)
                    {
                        next_rect = i;
                        break;
                    }
                }

                // If we didn't find a SpriteRule that fits, start a new row.
                if (next_rect < 0)
                {
                    y += row_hgt;
                    x = 0;
                    row_hgt = 0;
                    next_rect = 0;
                }

                // Position the selected SpriteRule.
                SpriteRule rect = not_positioned[next_rect];
                rect.X = x;
                rect.Y = y;
                x += rect.Width;
                if (row_hgt < rect.Height) row_hgt = rect.Height;

                // Move the SpriteRule into the positioned list.
                positioned.Add(rect);
                not_positioned.RemoveAt(next_rect);
            }

            // Prepare the results.
            for (int i = 0; i <= positioned.Count - 1; i++)
                rects[i] = positioned[i];
        }

        // Start the recursion.
        public void AlgFillByStripes(int bin_width, SpriteRule[] rects)
        {
            // Sort by height.
            SpriteRule[] best_rects = (SpriteRule[])rects.Clone();
            Array.Sort(best_rects, new SpriteHeightComparer());

            // Make variables to track and record the best solution.
            bool[] is_positioned = new bool[best_rects.Length];
            int num_unpositioned = best_rects.Length;

            // Fill by stripes.
            int max_y = 0;
            for (int i = 0; i <= rects.Length - 1; i++)
            {
                // See if this SpriteRule is positioned.
                if (!is_positioned[i])
                {
                    // Start a new stripe.
                    num_unpositioned -= 1;
                    is_positioned[i] = true;
                    best_rects[i].X = 0;
                    best_rects[i].Y = max_y;

                    FillBoundedArea(
                        best_rects[i].Width, bin_width, max_y,
                        max_y + best_rects[i].Height,
                        ref num_unpositioned, ref best_rects, ref is_positioned);

                    if (num_unpositioned == 0) break;
                    max_y += best_rects[i].Height;
                }
            }

            // Save the best solution.
            Array.Copy(best_rects, rects, rects.Length);
        }

        // Fill the unbounded area, trying to the smallest maximum Y coordinate.
        // Set the following for the best solution we find:
        //       xmin, xmax, etc.    - Bounds of the SpriteRule we are trying to fill.
        //       num_unpositioned    - The number of SpriteRules not yet positioned in this solution.
        //                             Used to control the recursion.
        //       rects()             - All SpriteRules for the problem, some positioned and others not. 
        //                             Initially this is the partial solution we are working from.
        //                             At end, this is the best solution we could find.
        //       is_positioned()     - Indicates which SpriteRules are positioned in this solution.
        //       max_y               - The largest Y value for this solution.
        private void FillUnboundedArea(
         int xmin, int xmax, int ymin,
         ref int num_unpositioned, ref SpriteRule[] rects, ref bool[] is_positioned)
        {
            if (num_unpositioned <= 0) return;

            // Save a copy of the solution so far.
            int best_num_unpositioned = num_unpositioned;
            SpriteRule[] best_rects = (SpriteRule[])rects.Clone();
            bool[] best_is_positioned = (bool[])is_positioned.Clone();

            // Currently we have no solution for this area.
            int best_max_y = int.MaxValue;

            // Loop through the available SpriteRules.
            for (int i = 0; i <= rects.Length - 1; i++)
            {
                // See if (this SpriteRule is not yet positioned and will fit.
                if (!is_positioned[i] &&
                    rects[i].Width <= xmax - xmin)
                {
                    // It will fit. Try it.
                    // **************************************************
                    // Divide the remaining area horizontally.
                    int test1_num_unpositioned = num_unpositioned - 1;
                    SpriteRule[] test1_rects = (SpriteRule[])rects.Clone();
                    bool[] test1_is_positioned = (bool[])is_positioned.Clone();
                    test1_rects[i].X = xmin;
                    test1_rects[i].Y = ymin;
                    test1_is_positioned[i] = true;

                    // Fill the area on the right.
                    FillBoundedArea(xmin + rects[i].Width, xmax, ymin, ymin + rects[i].Height,
                        ref test1_num_unpositioned, ref test1_rects, ref test1_is_positioned);
                    // Fill the area on the bottom.
                    FillUnboundedArea(xmin, xmax, ymin + rects[i].Height,
                        ref test1_num_unpositioned, ref test1_rects, ref test1_is_positioned);

                    // Learn about the test solution.
                    int test1_max_y =
                        MaxY(test1_rects, test1_is_positioned);

                    // See if (this is better than the current best solution.
                    if ((test1_num_unpositioned == 0) && (test1_max_y < best_max_y))
                    {
                        // The test is better. Save it.
                        best_max_y = test1_max_y;
                        best_rects = test1_rects;
                        best_is_positioned = test1_is_positioned;
                        best_num_unpositioned = test1_num_unpositioned;
                    }

                    // **************************************************
                    // Divide the remaining area vertically.
                    int test2_num_unpositioned = num_unpositioned - 1;
                    SpriteRule[] test2_rects = (SpriteRule[])rects.Clone();
                    bool[] test2_is_positioned = (bool[])is_positioned.Clone();
                    test2_rects[i].X = xmin;
                    test2_rects[i].Y = ymin;
                    test2_is_positioned[i] = true;

                    // Fill the area on the right.
                    FillUnboundedArea(xmin + rects[i].Width, xmax, ymin,
                        ref test2_num_unpositioned, ref test2_rects, ref test2_is_positioned);
                    // Fill the area on the bottom.
                    FillUnboundedArea(xmin, xmin + rects[i].Width, ymin + rects[i].Height,
                        ref test2_num_unpositioned, ref test2_rects, ref test2_is_positioned);

                    // Learn about the test solution.
                    int test2_max_y =
                        MaxY(test2_rects, test2_is_positioned);

                    // See if (this is better than the current best solution.
                    if ((test2_num_unpositioned == 0) && (test2_max_y < best_max_y))
                    {
                        // The test is better. Save it.
                        best_max_y = test2_max_y;
                        best_rects = test2_rects;
                        best_is_positioned = test2_is_positioned;
                        best_num_unpositioned = test2_num_unpositioned;
                    }
                } // End trying this SpriteRule.
            } // End looping through the SpriteRules.

            // Return the best solution we found.
            is_positioned = best_is_positioned;
            num_unpositioned = best_num_unpositioned;
            rects = best_rects;
        }

        // Use SpriteRules to fill the given sub-area.
        // Set the following for the best solution we find:
        //       xmin, xmax, etc.    - Bounds of the SpriteRule we are trying to fill.
        //       num_unpositioned    - The number of SpriteRules not yet positioned in this solution.
        //                             Used to control the recursion.
        //       rects()             - All SpriteRules for the problem, some positioned and others not. 
        //                             Initially this is the partial solution we are working from.
        //                             At end, this is the best solution we could find.
        //       is_positioned()     - Indicates which SpriteRules are positioned in this solution.
        //       max_y               - The largest Y value for this solution.
        private void FillBoundedArea(
            int xmin, int xmax, int ymin, int ymax,
            ref int num_unpositioned, ref SpriteRule[] rects, ref bool[] is_positioned)
        {
            // See if every SpriteRule has been positioned.
            if (num_unpositioned <= 0) return;

            // Save a copy of the solution so far.
            int best_num_unpositioned = num_unpositioned;
            SpriteRule[] best_rects = (SpriteRule[])rects.Clone();
            bool[] best_is_positioned = (bool[])is_positioned.Clone();

            // Currently we have no solution for this area.
            double best_density = 0;

            // Some SpriteRules have not been positioned.
            // Loop through the available SpriteRules.
            for (int i = 0; i <= rects.Length - 1; i++)
            {
                // See if this SpriteRule is not position and will fit.
                if ((!is_positioned[i]) &&
                    (rects[i].Width <= xmax - xmin) &&
                    (rects[i].Height <= ymax - ymin))
                {
                    // It will fit. Try it.
                    // **************************************************
                    // Divide the remaining area horizontally.
                    int test1_num_unpositioned = num_unpositioned - 1;
                    SpriteRule[] test1_rects = (SpriteRule[])rects.Clone();
                    bool[] test1_is_positioned = (bool[])is_positioned.Clone();
                    test1_rects[i].X = xmin;
                    test1_rects[i].Y = ymin;
                    test1_is_positioned[i] = true;

                    // Fill the area on the right.
                    FillBoundedArea(xmin + rects[i].Width, xmax, ymin, ymin + rects[i].Height,
                        ref test1_num_unpositioned, ref test1_rects, ref test1_is_positioned);
                    // Fill the area on the bottom.
                    FillBoundedArea(xmin, xmax, ymin + rects[i].Height, ymax,
                        ref test1_num_unpositioned, ref test1_rects, ref test1_is_positioned);

                    // Learn about the test solution.
                    double test1_density =
                        SolutionDensity(
                            xmin + rects[i].Width, xmax, ymin, ymin + rects[i].Height,
                            xmin, xmax, ymin + rects[i].Height, ymax,
                            test1_rects, test1_is_positioned);

                    // See if this is better than the current best solution.
                    if (test1_density >= best_density)
                    {
                        // The test is better. Save it.
                        best_density = test1_density;
                        best_rects = test1_rects;
                        best_is_positioned = test1_is_positioned;
                        best_num_unpositioned = test1_num_unpositioned;
                    }

                    // **************************************************
                    // Divide the remaining area vertically.
                    int test2_num_unpositioned = num_unpositioned - 1;
                    SpriteRule[] test2_rects = (SpriteRule[])rects.Clone();
                    bool[] test2_is_positioned = (bool[])is_positioned.Clone();
                    test2_rects[i].X = xmin;
                    test2_rects[i].Y = ymin;
                    test2_is_positioned[i] = true;

                    // Fill the area on the right.
                    FillBoundedArea(xmin + rects[i].Width, xmax, ymin, ymax,
                        ref test2_num_unpositioned, ref test2_rects, ref test2_is_positioned);
                    // Fill the area on the bottom.
                    FillBoundedArea(xmin, xmin + rects[i].Width, ymin + rects[i].Height, ymax,
                        ref test2_num_unpositioned, ref test2_rects, ref test2_is_positioned);

                    // Learn about the test solution.
                    double test2_density =
                        SolutionDensity(
                            xmin + rects[i].Width, xmax, ymin, ymax,
                            xmin, xmin + rects[i].Width, ymin + rects[i].Height, ymax,
                            test2_rects, test2_is_positioned);

                    // See if this is better than the current best solution.
                    if (test2_density >= best_density)
                    {
                        // The test is better. Save it.
                        best_density = test2_density;
                        best_rects = test2_rects;
                        best_is_positioned = test2_is_positioned;
                        best_num_unpositioned = test2_num_unpositioned;
                    }
                } // End trying this SpriteRule.
            } // End looping through the SpriteRules.

            // Return the best solution we found.
            is_positioned = best_is_positioned;
            num_unpositioned = best_num_unpositioned;
            rects = best_rects;
        }

        // Find the largest Y coordinate in the solution.
        private int MaxY(SpriteRule[] rects, bool[] is_positioned)
        {
            int max_y = 0;
            for (int i = 0; i <= rects.Length - 1; i++)
                if (is_positioned[i] && (max_y < (rects[i].Y + rects[i].Height))) max_y = (rects[i].Y + rects[i].Height);
            return max_y;
        }

        // Find the largest Y coordinate in all SpriteRules.
        private int MaxY(SpriteRule[] rects)
        {
            int max_y = 0;
            for (int i = 0; i <= rects.Length - 1; i++)
                if (max_y < (rects[i].Y + rects[i].Height)) max_y = (rects[i].Y + rects[i].Height);
            return max_y;
        }

        // Find the density of the SpriteRules in the given areas for this solution.
        private double SolutionDensity(
            int xmin1, int xmax1, int ymin1, int ymax1,
            int xmin2, int xmax2, int ymin2, int ymax2,
            SpriteRule[] rects, bool[] is_positioned)
        {
            var rect1 = new Rectangle(xmin1, ymin1, xmax1 - xmin1, ymax1 - ymin1);
            var rect2 = new Rectangle(xmin2, ymin2, xmax2 - xmin2, ymax2 - ymin2);
            int area_covered = 0;
            for (int i = 0; i <= rects.Length - 1; i++)
            {
                if (is_positioned[i] &&
                    (rects[i].IntersectsWith(rect1) ||
                     rects[i].IntersectsWith(rect2)))
                {
                    area_covered += rects[i].Width * rects[i].Height;
                }
            }

            double denom = rect1.Width * rect1.Height + rect2.Width * rect2.Height;
            if (System.Math.Abs(denom) < 0.001) return 0;

            return area_covered / denom;
        }

        // Start the recursion.
        public void AlgRecursiveDivision(int bin_width, SpriteRule[] rects)
        {
            // Sort by height.
            SpriteRule[] best_solution = (SpriteRule[])rects.Clone();
            Array.Sort(rects, new SpriteHeightComparer());

            // Make variables to track and record the best solution.
            bool[] is_positioned = new bool[rects.Length];
            int num_unpositioned = rects.Length;

            // Perform the recursion.
            FillUnboundedArea(0, bin_width, 0, ref num_unpositioned, ref best_solution, ref is_positioned);

            // Save the best solution.
            Array.Copy(best_solution, rects, rects.Length);
        }

        private void FillExhaustive(ref SpriteRule[] best_rects, ref int best_max_y, SpriteRule[] rects, int next_i, bool[,] used)
        {
            // See if (we//re at a leaf node.
            if (next_i >= rects.Length)
            {
                // We//re at a leaf node. See if (this solution is an improvement.
                int max_y = MaxY(rects);
                if (best_max_y > max_y)
                {
                    best_max_y = max_y;
                    best_rects = (SpriteRule[])rects.Clone();
                }
            }
            else
            {
                SpriteRule rect = rects[next_i];
                for (int y = 0; y <= used.GetUpperBound(1) - rect.Height + 1; y++)
                {
                    for (int x = 0; x <= used.GetUpperBound(0) - rect.Width + 1; x++)
                    {
                        // See if (we can put the SpriteRule here.
                        bool can_fit = true;
                        for (int i = 0; i <= rect.Width - 1; i++)
                        {
                            for (int j = 0; j <= rect.Height - 1; j++)
                            {
                                if (used[x + i, y + j])
                                {
                                    can_fit = false;
                                    break;
                                }
                            }
                            if (!can_fit) break;
                        }

                        // Give it a try.
                        if (can_fit)
                        {
                            // Put the SpriteRule here.
                            rects[next_i].X = x;
                            rects[next_i].Y = y;
                            for (int i = 0; i <= rect.Width - 1; i++)
                            {
                                for (int j = 0; j <= rect.Height - 1; j++)
                                {
                                    used[x + i, y + j] = true;
                                }
                            }

                            // Recurse.
                            FillExhaustive(ref best_rects, ref best_max_y, rects, next_i + 1, used);

                            // Un-put the SpriteRule here.
                            for (int i = 0; i <= rect.Width - 1; i++)
                            {
                                for (int j = 0; j <= rect.Height - 1; j++)
                                {
                                    used[x + i, y + j] = false;
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    internal interface ISpriteRectangle
    {
        int Width { get; }
        int Height { get; }
        int X { get; set; }
        int Y { get; set; }
    }

    internal static class SpriteRectangleExtender
    {
        public static bool IntersectsWith(this ISpriteRectangle ths, Rectangle rect)
        {
            return ((((rect.X < (ths.X + ths.Width)) && (ths.X < (rect.X + rect.Width))) && (rect.Y < (ths.Y + ths.Height))) && (ths.Y < (rect.Y + rect.Height)));
        }
    }
}
