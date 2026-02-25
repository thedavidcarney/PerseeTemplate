using UnityEngine;
using System;

namespace DepthProcessing
{
    public abstract class DepthParameter
    {
        public string DisplayName { get; protected set; }
        public abstract string DisplayValue { get; }
        public virtual void Increment() { }
        public virtual void Decrement() { }
        public virtual void Toggle() { }
    }

    public class FloatParameter : DepthParameter
    {
        private Func<float> getter;
        private Action<float> setter;
        private float step;
        private float min;
        private float max;

        public override string DisplayValue => getter().ToString("F3");

        public FloatParameter(string name, Func<float> getter, Action<float> setter, float step, float min, float max)
        {
            DisplayName = name;
            this.getter = getter;
            this.setter = setter;
            this.step = step;
            this.min = min;
            this.max = max;
        }

        public override void Increment() => setter(Mathf.Clamp(getter() + step, min, max));
        public override void Decrement() => setter(Mathf.Clamp(getter() - step, min, max));
    }

    public class IntParameter : DepthParameter
    {
        private Func<int> getter;
        private Action<int> setter;
        private int min;
        private int max;

        public override string DisplayValue => getter().ToString();

        public IntParameter(string name, Func<int> getter, Action<int> setter, int min, int max)
        {
            DisplayName = name;
            this.getter = getter;
            this.setter = setter;
            this.min = min;
            this.max = max;
        }

        public override void Increment() => setter(Mathf.Clamp(getter() + 1, min, max));
        public override void Decrement() => setter(Mathf.Clamp(getter() - 1, min, max));
    }

    public class BoolParameter : DepthParameter
    {
        private Func<bool> getter;
        private Action<bool> setter;

        public override string DisplayValue => getter() ? "ON" : "OFF";

        public BoolParameter(string name, Func<bool> getter, Action<bool> setter)
        {
            DisplayName = name;
            this.getter = getter;
            this.setter = setter;
        }

        public override void Toggle() => setter(!getter());
        public override void Increment() => Toggle();
        public override void Decrement() => Toggle();
    }
}