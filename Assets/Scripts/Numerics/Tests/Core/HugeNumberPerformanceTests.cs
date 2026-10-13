using System;
using NUnit.Framework;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;
using HugeNumberSystem.Numerics;

namespace HugeNumberSystem.Numerics.Tests
{
    public sealed class HugeNumberPerformanceTests
    {
        private static int s_orderingSink;

        [Test]
        public void HotPathOperationsDoNotAllocateManagedMemory()
        {
            HugeNumber left = HugeNumber.FromParts(1.25d, 42L);
            HugeNumber right = HugeNumber.FromParts(2.5d, 17L);

            WarmHotPaths(left, right);

            Assert.That(() =>
            {
                int ordering = 0;
                for (int i = 0; i < 1000; i++)
                {
                    HugeNumber constructed = HugeNumber.FromParts(1.25d, 42L);
                    HugeNumber sum = left + right;
                    HugeNumber difference = left - right;
                    HugeNumber product = left * right;
                    HugeNumber quotient = left / right;
                    HugeNumber power = HugeNumber.Pow(left, 3L);
                    HugeNumber realPower = HugeNumber.Pow(left, 1.5d);
                    double logarithm = left.Log10();
                    ordering += constructed.CompareTo(left) + sum.CompareTo(product) +
                        difference.CompareTo(quotient) + power.CompareTo(realPower) +
                        logarithm.CompareTo(0d);
                }

                s_orderingSink = ordering;
            }, Is.Not.AllocatingGCMemory());
        }

        private static void WarmHotPaths(HugeNumber left, HugeNumber right)
        {
            HugeNumber constructed = HugeNumber.FromParts(1.25d, 42L);
            HugeNumber sum = left + right;
            HugeNumber difference = left - right;
            HugeNumber product = left * right;
            HugeNumber quotient = left / right;
            HugeNumber power = HugeNumber.Pow(left, 3L);
            HugeNumber realPower = HugeNumber.Pow(left, 1.5d);
            double logarithm = left.Log10();
            int ordering = constructed.CompareTo(left) + sum.CompareTo(product) +
                difference.CompareTo(quotient) + power.CompareTo(realPower) +
                logarithm.CompareTo(0d);
            s_orderingSink = ordering;
        }
    }
}
