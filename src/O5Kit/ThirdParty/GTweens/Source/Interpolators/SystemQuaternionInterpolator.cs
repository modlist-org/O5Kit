using GTweens.Easings;
using System.Numerics;

using System;
using System.Collections.Generic;
namespace GTweens.Interpolators;

public sealed class SystemQuaternionInterpolator : IInterpolator<Quaternion> {
    public static readonly SystemQuaternionInterpolator Instance = new();

    SystemQuaternionInterpolator() {

    }

    public Quaternion Evaluate(
        Quaternion initialValue,
        Quaternion finalValue,
        float time,
        EasingDelegate easingDelegate
        ) {
        float curveTime = easingDelegate(0f, 1f, time);

        return Quaternion.Slerp(initialValue, finalValue, curveTime);
    }

    public Quaternion Subtract(Quaternion initialValue, Quaternion finalValue) => Quaternion.Inverse(initialValue) * finalValue;

    public Quaternion Add(Quaternion initialValue, Quaternion finalValue) => initialValue * finalValue;
}