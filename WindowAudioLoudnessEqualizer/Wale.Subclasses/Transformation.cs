using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wale.Subclasses
{
    public static class Transformation
    {
        private static TransMode transMode;
        private static double BaseVolume;

        //Transformation methods between user and machine.
        public enum TransMode { Transform1, Transform2 };
        public enum TransFlow { UserToMachine, MachineToUser, IntervalUserToMachine, IntervalMachineToUser };

        public static void SetBaseVolume(double baseVolume) { BaseVolume = baseVolume; }
        public static void ChangeTransformMethod(TransMode t) { transMode = t; }
        public static double Transform(double input, TransFlow f)
        {
            switch (transMode)
            {
                case TransMode.Transform1: return Transform1(input, f);
                case TransMode.Transform2: return Transform2(input, f);
                default: return 0;
            }
        }

        private static double Transform1(double input, TransFlow mod)
        {
            switch (mod)
            {
                case TransFlow.UserToMachine: return ((input) * BaseVolume);
                case TransFlow.MachineToUser: return ((input / BaseVolume));
                case TransFlow.IntervalUserToMachine: return (input * BaseVolume);
                case TransFlow.IntervalMachineToUser: return (input / BaseVolume);
                default: return 0;
            }
        }/**/
        private static double Transform2(double input, TransFlow mod)
        {
            switch (mod)
            {
                case TransFlow.UserToMachine: return ((input + 1) / 2 * BaseVolume);
                case TransFlow.MachineToUser: return ((input * 2) - 1 / BaseVolume);
                case TransFlow.IntervalUserToMachine: return (input / 2 * BaseVolume);
                case TransFlow.IntervalMachineToUser: return (input * 2 / BaseVolume);
                default: return 0;
            }
        }/**/
        //Class ends
    }//class Transformaion
}
