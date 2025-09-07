using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class UpdateDriver : MonoBehaviour
{
    private List<IUpdateableService> updateables;

    void Start()
    {
        updateables = DIBootstrapper.Container.ResolveAll<IUpdateableService>().ToList();
    }

    void Update()
    {
        foreach (var u in updateables)
            u.Update();
    }
}
