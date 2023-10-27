using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Controls;

namespace Starward.Controls;

internal sealed class HandCursorGrid : Grid
{

    public HandCursorGrid()
    {
        ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Hand);
    }

}
