using DragonGlareAlpha.Domain;

namespace DragonGlareAlpha.Services;

public static class MapFactory
{
    public const int FloorTile = 0;
    public const int WallTile = 1;
    public const int CastleBlockTile = 2;
    public const int CastleGateTile = 3;
    public const int FieldGateTile = 4;
    public const int CastleFloorTile = 5;
    public const int GrassTile = 6;
    public const int DecorationBlueTile = 7;

    public static int[,] CreateDefaultMap()
    {
        return CreateMap(FieldMapId.Hub);
    }

    public static int[,] CreateMap(FieldMapId mapId)
    {
        return mapId switch
        {
            FieldMapId.Castle => CreateCastleMap(),
            FieldMapId.Field => CreateFieldMap(),
            _ => CreateHubMap()
        };
    }

    private static int[,] CreateHubMap()
    {
        var map = CreateBoundedMap();

        PaintArea(map, 9, 0, 10, 0, CastleGateTile);
        PaintArea(map, 19, 7, 19, 8, FieldGateTile);

        for (var x = 4; x <= 15; x++)
        {
            map[10, x] = WallTile;
        }

        map[10, 9] = FloorTile;
        map[10, 10] = FloorTile;
        map[6, 6] = WallTile;
        map[6, 7] = WallTile;
        map[7, 6] = WallTile;
        return map;
    }

    private static int[,] CreateCastleMap()
    {
        var map = CreateBoundedMap();

        PaintArea(map, 1, 1, 18, 13, CastleFloorTile);
        PaintArea(map, 7, 2, 12, 3, CastleBlockTile);
        PaintArea(map, 4, 4, 5, 11, WallTile);
        PaintArea(map, 14, 4, 15, 11, WallTile);
        PaintArea(map, 9, 14, 10, 14, CastleGateTile);
        return map;
    }

    private static int[,] CreateFieldMap()
    {
        var map = CreateBoundedMap();

        PaintArea(map, 0, 7, 0, 8, FieldGateTile);
        PaintArea(map, 2, 2, 6, 5, GrassTile);
        PaintArea(map, 10, 3, 16, 6, GrassTile);
        PaintArea(map, 5, 9, 12, 12, GrassTile);
        PaintArea(map, 8, 7, 9, 8, DecorationBlueTile);
        PaintArea(map, 14, 10, 15, 11, DecorationBlueTile);
        return map;
    }

    private static int[,] CreateBoundedMap()
    {
        var map = new int[15, 20];

        for (var y = 0; y < map.GetLength(0); y++)
        {
            for (var x = 0; x < map.GetLength(1); x++)
            {
                map[y, x] = x == 0 || y == 0 || x == map.GetLength(1) - 1 || y == map.GetLength(0) - 1
                    ? WallTile
                    : FloorTile;
            }
        }

        return map;
    }

    private static void PaintArea(int[,] map, int left, int top, int right, int bottom, int tile)
    {
        for (var y = top; y <= bottom; y++)
        {
            for (var x = left; x <= right; x++)
            {
                map[y, x] = tile;
            }
        }
    }
}
