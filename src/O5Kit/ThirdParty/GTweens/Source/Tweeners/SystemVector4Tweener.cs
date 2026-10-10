using GTweens.Delegates;
using GTweens.Interpolators;
using System.Numerics;

using System;
using System.Collections.Generic;
namespace GTweens.Tweeners;

public sealed class SystemVector4Tweener : Tweener<Vector4> {
    public SystemVector4Tweener(
        Getter currValueGetter,
        Setter setter,
        Getter to,
        float duration,
        ValidationDelegates.Validation validation
    )
        : base(
            currValueGetter,
            setter,
            to,
            duration,
            SystemVector4Interpolator.Instance,
            validation
        ) {
    }
}