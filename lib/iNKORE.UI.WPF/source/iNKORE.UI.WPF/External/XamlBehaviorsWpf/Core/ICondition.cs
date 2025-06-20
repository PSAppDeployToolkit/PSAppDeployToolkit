// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
namespace Microsoft.Xaml.Behaviors.Core
{
    /// <summary>
    /// An interface that a given object must implement in order to be 
    /// set on a ConditionBehavior.Condition property. 
    /// </summary>
    public interface ICondition
    {
        bool Evaluate();
    }
}
