using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ECS;
using FlatBuffers;
using PatchZone.Hatch;
using PatchZone.Hatch.Annotations;

namespace CreativeZone.Services
{
    class CreativeEntityManager : ProxyService<CreativeEntityManager, IEntityManager>
    {
        public int SuppressDestroyEntity;

        public event Action<UID> OnEntityDestroyed;

        [LogicProxy]
        public void DestroyEntity(ref UID entity)
        {
            if(this.SuppressDestroyEntity == 0)
            {
                var id = entity;
                this.Vanilla.DestroyEntity(ref entity);
                this.OnEntityDestroyed?.Invoke(id);
            }
        }

        private readonly Queue<Action> InvokeQueue = new Queue<Action>();

        [LogicProxy]
        public void Tick(float deltaTime)
        {
            this.Vanilla.Tick(deltaTime);

            var count = this.InvokeQueue.Count;
            for(int i = 0; i < count; i++)
            {
                this.InvokeQueue.Dequeue().Invoke();
            }
        }

        public void Invoke(Action action)
        {
            this.InvokeQueue.Enqueue(action);
        }
    }
}
