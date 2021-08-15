using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace jiratg_bot {

    /**
     * простейшая FSM: работает на базе какого-то enum, умеет методы
     * private void XxxxTransition(State previous) - переход в состояние Xxxxx
     * и private void XxxxxState(Update update) - обработка состояния Xxxx
     * ...Transision и ....State могут быть async
     */
    public class FSM<T> where T : struct, IConvertible {

        public T State { get; private set; }

        private const BindingFlags FLAGS = BindingFlags.NonPublic | BindingFlags.Instance;

        private IDictionary<T, MethodInfo> states = new Dictionary<T, MethodInfo>();
        private IDictionary<T, MethodInfo> transitions = new Dictionary<T, MethodInfo>();

        public FSM(T init) {
            if(!typeof(T).IsEnum) {
                throw new ArgumentException("T must be an enumeration");
            }

            // Cache state and transition functions
            foreach(T value in typeof(T).GetEnumValues()) {
                var s = GetType().GetMethod(value.ToString() + "State", FLAGS);
                if(s != null) {
                    states.Add(value, s);
                }

                var t = GetType().GetMethod(value.ToString() + "Transition", FLAGS);
                if(t != null) {
                    transitions.Add(value, t);
                }
            }

            State = init;
        }

        public async Task Transition(T next)
        {
            MethodInfo method;
            if (transitions.TryGetValue(next, out method))
            {
                var task = (Task)method.Invoke(this, new object[] { State });
                if (task != null) await task;
            }

            State = next;
        }

        public async Task StateDo(Update update) {
            MethodInfo method;
            if (states.TryGetValue(State, out method))
            {
                var task = method.Invoke(this, new object[] { update }) as Task;
                if (task != null) await task;
            }
        }
    }
}