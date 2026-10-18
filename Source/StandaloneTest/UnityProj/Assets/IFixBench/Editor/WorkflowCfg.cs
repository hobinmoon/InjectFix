using System;
using System.Collections.Generic;
using IFix;
using IFixBench.Workflow;

//Types to inject (must be in an Editor folder and marked [Configure])
[Configure]
public class WorkflowCfg
{
    [IFix]
    static IEnumerable<Type> hotfix
    {
        get
        {
            return new List<Type>()
            {
                typeof(WfCalculator),
                typeof(WfMath),
                typeof(WfMover),
                typeof(WfInventory),
                typeof(WfPlayer),
                typeof(WfReward),
                typeof(WfEnemyBase),
                typeof(WfOrc),
                typeof(WfHot),
            };
        }
    }
}
