using Microsoft.UI.Xaml.Media.Animation;
using System;

namespace Starward.Messages;

public record MainPageNavigateMessage(Type Page, object? Param = null, NavigationTransitionInfo? TransitionInfo = null);

