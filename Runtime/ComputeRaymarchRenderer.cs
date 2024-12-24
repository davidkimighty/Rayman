using System.Collections.Generic;
using UnityEngine;

namespace Rayman
{
    public class ComputeRaymarchRenderer : MonoBehaviour, IRaymarchRendererControl
    {
        [SerializeField] protected List<RaymarchShape> shapes = new();

        public List<RaymarchShape> Shapes => shapes;
        
        public void AddShape(RaymarchShape shape)
        {
            throw new System.NotImplementedException();
        }

        public void RemoveShape(RaymarchShape shape)
        {
            throw new System.NotImplementedException();
        }
    }
}