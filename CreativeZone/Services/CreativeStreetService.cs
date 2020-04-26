using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using CreativeZone.Utils;
using FlatBuffers;
using PatchZone.Hatch;
using PatchZone.Hatch.Annotations;
using Service.Audio;
using Service.Building;
using Service.Grid;
using Service.Street;
using Service.UserInput;

namespace CreativeZone.Services
{
    class CreativeStreetService : ProxyService<CreativeStreetService, CreativeStreetService.IExtendedStreetService>
    {
        public interface IExtendedStreetService : IStreetService
        {
            bool HasStreet(int cellX, int cellY);
            bool CanBuildStreet(int cellX, int cellY);
        }

        [LogicProxy]
        public int? StreetStartX { get; set; }

        [LogicProxy]
        public int? StreetStartY { get; set; }

        private StreetPlanMode PlanningMode = StreetPlanMode.X;

        private int CurrentPlanEndX;
        private int CurrentPlanEndY;
        private int CurrentPlanStartX;
        private int CurrentPlanStartY;
        private StreetPlanMode? CurrentPlanMode;

        enum StreetPlanMode
        {
            X,
            Y,
            Diagonal,
            Fill,

            MODE_COUNT,
        }

        [LogicProxy]
        public void AcceptStreetPlan(List<StreetPosition> streetPath = null)
        {
            if(streetPath == null)
            {
                if(this.Vanilla.IsPlanning() == false)
                {
                    return;
                }

                streetPath = this.Vanilla.GetCurrentPlan();

                if(streetPath.Count == 0)
                    return;
            }
            
            foreach(var tile in streetPath)
            {
                if(tile.hasStreet)
                {
                    this.Vanilla.RemoveStreet(tile.x, tile.y);
                }

                if(tile.canBuildStreet || tile.hasStreet)
                {
                    this.Vanilla.CreateStreet(tile.x, tile.y, BuildingType.RoofedStreet);
                }
            }

            this.StreetStartX = this.CurrentPlanEndX;
            this.StreetStartY = this.CurrentPlanEndY;

            ServiceMapper.audioService.PlaySoundUI(SoundClip.Misc_PlaceBuilding);
        }

        [LogicProxy]
        private void FindStreetPath(int startCellX, int startCellY, int endCellX, int endCellY)
        {
            if (ServiceMapper.userInputService.ButtonUp(Button.RotateBuildingKeyboard))
            {
                var nextMode = this.PlanningMode + 1;

                if (nextMode >= StreetPlanMode.MODE_COUNT)
                {
                    nextMode = StreetPlanMode.X;
                }

                this.PlanningMode = nextMode;
            }

            var xDiff = endCellX - startCellX;
            var yDiff = endCellY - startCellY;

            var xDiffAbs = Math.Abs(xDiff);
            var yDiffAbs = Math.Abs(yDiff);

            var xSign = Math.Sign(xDiff);
            var ySign = Math.Sign(yDiff);

            if(this.PlanningMode == StreetPlanMode.X || this.PlanningMode == StreetPlanMode.Y)
            {
                if(xDiffAbs < 2 && yDiffAbs < 2 && xDiffAbs != yDiffAbs)
                {
                    this.PlanningMode = xDiffAbs > yDiffAbs ? StreetPlanMode.X : StreetPlanMode.Y;
                }
            }

            if
            ((
                this.CurrentPlanEndX.Set(endCellX) |
                this.CurrentPlanEndY.Set(endCellY) |
                this.CurrentPlanStartX.Set(startCellX) |
                this.CurrentPlanStartY.Set(startCellY) |
                this.CurrentPlanMode != this.PlanningMode
            ) == false)
            {
                return;
            }

            this.CurrentPlanMode = this.PlanningMode;

            var plan = this.Vanilla.GetCurrentPlan();
            plan.Clear();

            var x = startCellX;
            var y = startCellY;

            var strategy = this.PlanningMode;
            if (xDiff == 0)
            {
                if(yDiff != 0)
                {
                    strategy = StreetPlanMode.Y;
                }
                else
                {
                    strategy = StreetPlanMode.Fill;
                }
            }
            else if (yDiff == 0)
            {
                strategy = StreetPlanMode.X;
            }

            if (strategy == StreetPlanMode.X)
            {
                x -= xSign;
                for (var end = endCellX; x != end;)
                {
                    x += xSign;
                    Add(x, y);
                }

                y += ySign;
                for (var end = endCellY + ySign; y != end; y += ySign)
                {
                    Add(x, y);
                }
            }
            else if (strategy == StreetPlanMode.Y)
            {
                y -= ySign;
                for (var end = endCellY; y != end;)
                {
                    y += ySign;
                    Add(x, y);
                }

                x += xSign;
                for (var end = endCellX + xSign; x != end; x += xSign)
                {
                    Add(x, y);
                }
            }
            else if(strategy == StreetPlanMode.Fill)
            {
                if(startCellX > endCellX)
                {
                    startCellX.SwapWith(ref endCellX);
                }

                if (startCellY > endCellY)
                {
                    startCellY.SwapWith(ref endCellY);
                }

                for(x = startCellX; x <= endCellX; x++)
                for(y = startCellY; y <= endCellY; y++)
                {
                    Add(x, y);
                }
            }
            else if(strategy == StreetPlanMode.Diagonal)
            {
                float deltaErr = Math.Abs((float)yDiff / (float)xDiff);

                float error = 0;
                for (var end = endCellX + xSign; x != end; x += xSign)
                {
                    Add(x, y);
                    error += deltaErr;

                    while(error >= 0.5f && y != endCellY)
                    {
                        y += ySign;
                        Add(x, y);
                        error -= 1.0f;
                    }
                }
            }

            //Note: Vanilla `FindStreetPath` is bugged and produces one duplicate tile.
            //      GUI then compensates for this expected flaw so we need to replicate it as well.
            plan.Add(plan[0]);

            void Add(int _x, int _y)
            {
                plan.Add(new StreetPosition
                (
                    _x, 
                    _y, 
                    this.Vanilla.CanBuildStreet(_x, _y), 
                    this.Vanilla.HasStreet(_x, _y)
                ));
            }
        }
    }
}
