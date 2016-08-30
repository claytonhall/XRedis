using System;

namespace RedistestConsole
{
    public class ScanBuilder<T>
        where T : new()
    {
        public ScanBuilder(WorkArea<T> workArea)
        {
            this.WorkArea = WorkArea;
        }

        public WorkArea<T> WorkArea { get; set; }

        public void Invoke(Action<WorkArea<T>> action)
        {
            action(this.WorkArea);
        }
    }
}