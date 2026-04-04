namespace MorrowindMenusTrading.UI;

public static class TimeControlsStyleState
{
    private static int depth;

    public static bool Active => depth > 0;

    public static void Push()
    {
        depth++;
    }

    public static void Pop()
    {
        if (depth > 0)
        {
            depth--;
        }
    }
}
