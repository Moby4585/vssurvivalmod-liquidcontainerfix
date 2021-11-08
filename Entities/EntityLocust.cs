﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace Vintagestory.GameContent
{
    public class EntityLocust : EntityGlowingAgent
    {
        double mul1, mul2;

        /// <summary>
        /// Gets the walk speed multiplier.
        /// </summary>
        /// <param name="groundDragFactor">The amount of drag provided by the current ground. (Default: 0.3)</param>
        public override double GetWalkSpeedMultiplier(double groundDragFactor = 0.3)
        {
            double multiplier = (servercontrols.Sneak ? GlobalConstants.SneakSpeedMultiplier : 1.0) * (servercontrols.Sprint ? GlobalConstants.SprintSpeedMultiplier : 1.0);

            if (FeetInLiquid) multiplier /= 2.5;


            multiplier *= mul1 * mul2;

            // Apply walk speed modifiers.
            multiplier *= GameMath.Clamp(Stats.GetBlended("walkspeed"), 0, 999);

            return multiplier;
        }


        int cnt;

        public override void OnGameTick(float dt)
        {
            base.OnGameTick(dt);

            // Needed for GetWalkSpeedMultiplier(), less read those a little less often for performance
            if (cnt++ > 2)
            {
                cnt = 0;
                var pos = SidedPos;
                Block belowBlock = World.BlockAccessor.GetBlock((int)pos.X, (int)(pos.Y - 0.05f), (int)pos.Z);
                Block insideblock = World.BlockAccessor.GetBlock((int)pos.X, (int)(pos.Y + 0.01f), (int)pos.Z);

                mul1 = belowBlock.Code == null || belowBlock.Code.Path.Contains("metalspike") ? 1 : belowBlock.WalkSpeedMultiplier;
                mul2 = insideblock.Code == null || insideblock.Code.Path.Contains("metalspike") ? 1 : insideblock.WalkSpeedMultiplier;
            }
        }
    }
}
