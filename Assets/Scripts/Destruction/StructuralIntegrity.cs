using UnityEngine;
using System.Collections.Generic;

namespace DestructionRoyale.Destruction
{
    public static class StructuralIntegrity
    {
        public static bool[,] CalculateStability(bool[,] alive, int width, int height)
        {
            bool[,] stable = new bool[width, height];
            bool[,] visited = new bool[width, height];

            // Bottom row segments are always stable (grounded)
            for (int x = 0; x < width; x++)
            {
                if (alive[x, 0])
                {
                    FloodFill(x, 0, alive, visited, stable, width, height);
                }
            }

            return stable;
        }

        private static void FloodFill(int x, int y, bool[,] alive, bool[,] visited, bool[,] stable, int width, int height)
        {
            if (x < 0 || x >= width || y < 0 || y >= height) return;
            if (visited[x, y] || !alive[x, y]) return;

            visited[x, y] = true;
            stable[x, y] = true;

            FloodFill(x + 1, y, alive, visited, stable, width, height);
            FloodFill(x - 1, y, alive, visited, stable, width, height);
            FloodFill(x, y + 1, alive, visited, stable, width, height);
            FloodFill(x, y - 1, alive, visited, stable, width, height);
        }

        public static List<(int x, int y)> FindUnstableSegments(bool[,] alive, int width, int height)
        {
            bool[,] stable = CalculateStability(alive, width, height);
            List<(int x, int y)> unstable = new List<(int, int)>();

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (alive[x, y] && !stable[x, y])
                    {
                        unstable.Add((x, y));
                    }
                }
            }

            return unstable;
        }

        public static bool HasBreachPattern(bool[,] alive, int cx, int cy, int width, int height)
        {
            // Check if there's a breach pattern centered at (cx, cy)
            // A breach forms when 4+ connected segments are destroyed

            int destroyed = 0;

            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    int nx = cx + dx;
                    int ny = cy + dy;

                    if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                    {
                        if (!alive[nx, ny]) destroyed++;
                    }
                }
            }

            return destroyed >= 4;
        }

        public static List<(int startX, int startY, int endX, int endY)> FindBreaches(bool[,] alive, int width, int height)
        {
            List<(int, int, int, int)> breaches = new List<(int, int, int, int)>();
            bool[,] processed = new bool[width, height];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (!alive[x, y] && !processed[x, y])
                    {
                        // Find connected destroyed region
                        int minX = x, maxX = x, minY = y, maxY = y;
                        Queue<(int, int)> queue = new Queue<(int, int)>();
                        queue.Enqueue((x, y));
                        processed[x, y] = true;
                        int count = 0;

                        while (queue.Count > 0)
                        {
                            var (cx, cy) = queue.Dequeue();
                            count++;
                            minX = Mathf.Min(minX, cx);
                            maxX = Mathf.Max(maxX, cx);
                            minY = Mathf.Min(minY, cy);
                            maxY = Mathf.Max(maxY, cy);

                            int[] dx = { 1, -1, 0, 0 };
                            int[] dy = { 0, 0, 1, -1 };

                            for (int d = 0; d < 4; d++)
                            {
                                int nx = cx + dx[d];
                                int ny = cy + dy[d];

                                if (nx >= 0 && nx < width && ny >= 0 && ny < height
                                    && !alive[nx, ny] && !processed[nx, ny])
                                {
                                    processed[nx, ny] = true;
                                    queue.Enqueue((nx, ny));
                                }
                            }
                        }

                        if (count >= 2)
                        {
                            breaches.Add((minX, minY, maxX, maxY));
                        }
                    }
                }
            }

            return breaches;
        }
    }
}
