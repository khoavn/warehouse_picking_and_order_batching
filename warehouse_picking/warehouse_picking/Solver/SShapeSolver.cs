﻿
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace warehouse_picking.Solver
{
    internal class SShapeSolver : ISolver
    {
        private Warehouse Warehouse { get; set; }
        private IClientWish ClientWish { get; set; }

        public SShapeSolver(Warehouse currentWarehouse, IClientWish currentClientWish)
        {
            Warehouse = currentWarehouse;
            ClientWish = currentClientWish;
        }

        public ISolution Solve()
        {
            var isLastDirectionUp = false;
            var wishesByAislesIdx = new List<ClientWishPos>[Warehouse.NbAisles];
            for (int i = 0; i < wishesByAislesIdx.Length; i++)
            {
                wishesByAislesIdx[i] = new List<ClientWishPos>();
            }
            foreach (var clientWish in ClientWish.ClientWishes)
            {
                var arrayIdx = clientWish.AislesIdx - 1;
                wishesByAislesIdx[arrayIdx].Add(clientWish);
            }
            var solution = new DummySolution();
            solution.Color = Color.Blue;
            var initShiftPoint = new ShiftPoint(0, 0);
            var shiftPointList = new List<ShiftPoint> {initShiftPoint};
            for (int i = 0; i < wishesByAislesIdx.Length; i = i + 2)
            {
                var wishes = new List<ClientWishPos>(wishesByAislesIdx[i]);
                if (i + 1 < wishesByAislesIdx.Length)
                {
                    var wishesBonus = wishesByAislesIdx[i + 1];
                    wishes.AddRange(wishesBonus);
                }
                if (wishes.Count == 0)
                {
                    continue;
                }
                var shiftPoints = OrderWishesByAisle(wishes, isLastDirectionUp);
                var lastShiftPoint = shiftPoints.Last();
                if (isLastDirectionUp)
                {
                    // add bottom path
                    var bottomWish = new ShiftPoint(lastShiftPoint.X, 0);
                    shiftPoints.Add(bottomWish);
                }
                else
                {
                    var wishY = (Warehouse.NbBlock - 1)*(Warehouse.AisleLenght + 2) + Warehouse.AisleLenght + 1;
                    // add top path
                    var topWish = new ShiftPoint(lastShiftPoint.X, wishY);
                    shiftPoints.Add(topWish);
                }
                AddIntermediatePositionIfNeeded(shiftPoints, shiftPointList);
                isLastDirectionUp = !isLastDirectionUp;
            }
            // this is the last aisles, we will go to the base
            var lastVisitedAisles = shiftPointList.Last();
            if (isLastDirectionUp)
            {
                var bottomOfLastAisles = new ShiftPoint(lastVisitedAisles.X, 0);
                shiftPointList.Add(bottomOfLastAisles);
            }
            shiftPointList.Add(initShiftPoint);
            solution.ShiftPointList = shiftPointList;
            return solution;
        }

        private static void AddIntermediatePositionIfNeeded(IEnumerable<ShiftPoint> orderWishes,
            List<ShiftPoint> shiftPointList)
        {
            var lastShiftPoint = shiftPointList.Last();
            foreach (var wish in orderWishes)
            {
                if (wish.X == lastShiftPoint.X || wish.Y == lastShiftPoint.Y)
                {
                    // only vertical or horizontal move, no add needed
                }
                else
                {
                    var intermdiateShiftPoint = new ShiftPoint(wish.X, lastShiftPoint.Y);
                    shiftPointList.Add(intermdiateShiftPoint);
                }
                var shiftPoint = new ShiftPoint(wish.X, wish.Y);
                shiftPointList.Add(shiftPoint);
                lastShiftPoint = wish;
            }
        }

        internal IList<ShiftPoint> OrderWishesByAisle(IEnumerable<ClientWishPos> wishes, bool isLastDirectionUp)
        {
            List<ClientWishPos> firstOrderedWishes;
            if (isLastDirectionUp)
            {
                firstOrderedWishes = wishes.OrderByDescending(w => w.BlockIdx)
                    .ThenByDescending(w => w.PositionIdx).ThenBy(w => w.AislesIdx).ToList();
            }
            else
            {
                firstOrderedWishes = wishes.OrderBy(w => w.BlockIdx)
                    .ThenBy(w => w.PositionIdx).ThenBy(w => w.AislesIdx).ToList();
            }
            if (firstOrderedWishes.Count == 1 || firstOrderedWishes.Count == 2)
            {
                return firstOrderedWishes.Select(c => c.ConverToShiftPoint()).ToList();
            }
            var secondOrderedWishes = new List<ClientWishPos>();
            int cpt = 0;
            while (cpt < firstOrderedWishes.Count() - 2)
            {
                // on desenlace si besoin 
                var wish1 = firstOrderedWishes[cpt];
                var wish2 = firstOrderedWishes[cpt + 1];
                var wish3 = firstOrderedWishes[cpt + 2];
                if (wish1.BlockIdx != wish2.BlockIdx)
                {
                    secondOrderedWishes.Add(wish1);
                    cpt++;
                    continue;
                }
                if (wish1.BlockIdx != wish3.BlockIdx)
                {
                    secondOrderedWishes.Add(wish1);
                    secondOrderedWishes.Add(wish2);
                    cpt += 2;
                    continue;
                }
                if (wish1.AislesIdx != wish2.AislesIdx
                    && wish1.AislesIdx == wish3.AislesIdx
                    && wish2.PositionIdx == wish3.PositionIdx)
                {
                    secondOrderedWishes.Add(wish1);
                    secondOrderedWishes.Add(wish3);
                    secondOrderedWishes.Add(wish2);
                    cpt += 3;
                }
                else
                {
                    secondOrderedWishes.Add(wish1);
                    cpt ++;
                }
            }
            for (int j = cpt; j < firstOrderedWishes.Count(); j++)
            {
                var wish = firstOrderedWishes[j];
                secondOrderedWishes.Add(wish);
            }
            return secondOrderedWishes.Select(c => c.ConverToShiftPoint()).ToList();
        }
    }
}