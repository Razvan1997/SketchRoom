using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace SketchRoom.DataStructures.Timming
{
    public class Timming
    {
        private readonly System.Timers.Timer _timer;
        private readonly bool _autoRestart;
        private readonly double _originalInterval;

        public event Action ElapsedAction;
        public event Action CompletedAction;

        private int _tickCount;
        private int? _maxTicks;

        public Timming(double intervalMs, bool autoRestart = true, int? maxTicks = null)
        {
            _originalInterval = intervalMs;
            _autoRestart = autoRestart;
            _maxTicks = maxTicks;

            _timer = new System.Timers.Timer(intervalMs);
            _timer.Elapsed += OnElapsed;
            _timer.AutoReset = autoRestart;
        }

        private void OnElapsed(object sender, ElapsedEventArgs e)
        {
            _tickCount++;
            ElapsedAction?.Invoke();

            if (_maxTicks.HasValue && _tickCount >= _maxTicks.Value)
            {
                Stop();
                CompletedAction?.Invoke();
            }
        }

        public void Start()
        {
            _tickCount = 0;
            _timer.Start();
        }

        public void Stop()
        {
            _timer.Stop();
        }

        public void Reset()
        {
            Stop();
            _tickCount = 0;
        }

        public void ChangeInterval(double milliseconds)
        {
            _timer.Interval = milliseconds;
        }

        public bool IsRunning => _timer.Enabled;




//        var infiniteTimer = new AppTimer(1000); // default: autoRestart = true, maxTicks = null

//    infiniteTimer.ElapsedAction += () =>
//{
//    Console.WriteLine("Tic nesfârșit...");
//};

//infiniteTimer.Start();
    }
}
