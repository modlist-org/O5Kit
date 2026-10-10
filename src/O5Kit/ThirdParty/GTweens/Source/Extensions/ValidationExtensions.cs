using GTweens.Delegates;

using System;
using System.Collections.Generic;
namespace GTweens.Extensions;

public static class ValidationExtensions {
    public static readonly ValidationDelegates.Validation AlwaysValid = () => true;
}