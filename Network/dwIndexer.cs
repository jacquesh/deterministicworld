using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DeterministicWorld.Network
{
    internal class dwIndexer<T> where T : dwIIdentifiable
    {
        private T[] indexedObjects;
        private Queue<int> freeIds;
        private int nextId;
        private int maxId;

        public dwIndexer()
        {
            indexedObjects = new T[10];
            freeIds = new Queue<int>();
            maxId = -1;
        }

        public T getObject(int obj_id)
        {
            if (obj_id < 0 || obj_id > maxId)
            {
                throw new System.IndexOutOfRangeException("Attempt to get the object for an invalid object ID - "+obj_id);
            }

            if (indexedObjects[obj_id] == null)
            {
                throw new System.IndexOutOfRangeException("OMGWTFBQQ null indexed object at index " + obj_id + "? We have " + indexedObjects.Length+" objects currently");
            }

            return indexedObjects[obj_id];
        }

        internal void indexObject(T obj)
        {
            if (obj == null)
                throw new System.IndexOutOfRangeException("OMGWTFBQQ trying to index null object?");
                //return;

            if (freeIds.Count == 0)
            {
                obj.id = nextId;
                nextId++;

                maxId = obj.id;
            }
            else
            {
                obj.id = freeIds.Dequeue();
            }

            //Resize if necessary
            if (obj.id >= indexedObjects.Length)
            {
                T[] newIndexedObjects = new T[indexedObjects.Length * 2];
                indexedObjects.CopyTo(newIndexedObjects, 0);

                indexedObjects = newIndexedObjects;
            }

            indexedObjects[obj.id] = obj;
            dwLog.info(typeof(T).Name+": Allocate " + obj.id);
        }

        internal void deindexObject(T obj)
        {
            if (obj.id == nextId - 1)
            {
                nextId--;
            }
            else
            {
                freeIds.Enqueue(obj.id);
            }

            dwLog.info(typeof(T).Name + ": Deallocate " + obj.id + ", freeID count: " + freeIds.Count);
            indexedObjects[obj.id] = default(T);
            obj.id = -1;
        }

    }
}
