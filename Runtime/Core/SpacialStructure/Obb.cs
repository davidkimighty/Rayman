using UnityEngine;

namespace Rayman
{
    public struct Obb : IBounds<Obb>
    {
        public bool Contains(Obb other)
        {
            throw new System.NotImplementedException();
        }

        public bool Intersects(Obb other)
        {
            throw new System.NotImplementedException();
        }

        public float HalfArea()
        {
            throw new System.NotImplementedException();
        }

        public Vector3 Center()
        {
            throw new System.NotImplementedException();
        }

        public Vector3 Extents()
        {
            throw new System.NotImplementedException();
        }

        public Obb Expand(float size)
        {
            throw new System.NotImplementedException();
        }

        public Obb Expand(Vector3 size)
        {
            throw new System.NotImplementedException();
        }

        public Obb Include(Vector3 point)
        {
            throw new System.NotImplementedException();
        }

        public Obb Union(Obb other)
        {
            throw new System.NotImplementedException();
        }
    }
}