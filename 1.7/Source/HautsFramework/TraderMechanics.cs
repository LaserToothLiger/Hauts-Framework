using System;
using System.Collections.Generic;
using System.Text;
using Verse;

namespace HautsFramework
{
    //a pawn whose traderkinddef has this DME can be traded with even if they don't have anything that their trader kind WillTrade
    public class DoesntHaveToSellAnythingToTrade : DefModExtension { }
}
