using Starward.Core.HoYoPlay;
using System;

namespace Starward.Features.ViewHost;

internal record class MainViewNavigateMessage(Type Page, GameId? GameId = null);
