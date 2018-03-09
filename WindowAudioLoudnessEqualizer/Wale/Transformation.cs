using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wale
{
    public static class Transformation
    {
        private static TransMode transMode;
        private static double BaseLevel;

        //Transformation methods between user and machine.
        /// <summary>
        /// Transform modes
        /// </summary>
        public enum TransMode { Transform1, Transform2 };
        /// <summary>
        /// Transform directions
        /// </summary>
        public enum TransFlow { UserToMachine, MachineToUser, IntervalUserToMachine, IntervalMachineToUser };

        public static void SetBaseLevel(double baseLevel) { BaseLevel = baseLevel; }
        /// <summary>
        /// Change transform mode to <paramref name="tMode"/>
        /// </summary>
        /// <param name="tMode">Target transform mode</param>
        public static void ChangeTransformMethod(TransMode tMode) { transMode = tMode; }
        /// <summary>
        /// Return transformed value from <paramref name="input"/> along <paramref name="flow"/> with current TransMode
        /// </summary>
        /// <param name="input"></param>
        /// <param name="flow">Direction of transformation</param>
        /// <returns></returns>
        public static double Transform(double input, TransFlow flow)
        {
            switch (transMode)
            {
                case TransMode.Transform1: return Transform1(input, flow);
                case TransMode.Transform2: return Transform2(input, flow);
                default: return 0;
            }
        }

        /// <summary>
        /// Return transformed value from <paramref name="input"/> along <paramref name="flow"/> with TransMode 1
        /// </summary>
        /// <param name="input"></param>
        /// <param name="flow">Direction of transformation</param>
        /// <returns></returns>
        private static double Transform1(double input, TransFlow flow)
        {
            switch (flow)
            {
                case TransFlow.UserToMachine: return ((input) * BaseLevel);
                case TransFlow.MachineToUser: return ((input / BaseLevel));
                case TransFlow.IntervalUserToMachine: return (input * BaseLevel);
                case TransFlow.IntervalMachineToUser: return (input / BaseLevel);
                default: return 0;
            }
        }/**/
        /// <summary>
        /// Return transformed value from <paramref name="input"/> along <paramref name="flow"/> with TransMode 2
        /// </summary>
        /// <param name="input"></param>
        /// <param name="flow">Direction of transformation</param>
        /// <returns></returns>
        private static double Transform2(double input, TransFlow flow)
        {
            switch (flow)
            {
                case TransFlow.UserToMachine: return ((input + 1) / 2 * BaseLevel);
                case TransFlow.MachineToUser: return ((input * 2) - 1 / BaseLevel);
                case TransFlow.IntervalUserToMachine: return (input / 2 * BaseLevel);
                case TransFlow.IntervalMachineToUser: return (input * 2 / BaseLevel);
                default: return 0;
            }
        }/**/
        //Class ends
    }//class Transformaion
}
