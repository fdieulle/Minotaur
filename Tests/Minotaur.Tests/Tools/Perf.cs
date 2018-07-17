using System;
using System.Diagnostics;
using System.Linq;

namespace Minotaur.Tests.Tools
{
    /// <summary>
    /// Helper class to measure performances.
    /// </summary>
    public static class Perf
    {
        #region Stopwatch extensions

        public static void TakeMeasure(this Stopwatch sw, string message)
        {
            sw.Stop();
            Console.WriteLine("{0} Elpased: {1} ms", message, sw.Elapsed.TotalMilliseconds);
            sw.Reset();
            sw.Start();
        }

        #endregion

        private const int DEFAULT_TIMES = 100;
        private const Units DEFAULT_UNITS = Units.Auto;
        private const int DEFAULT_JITTER_TIMES = 5;

        /// <summary>
        /// Measure a function by running it many times. By default 5 jitter run which will be ignore in the metrics then run 100 times.
        /// </summary>
        /// <param name="action">Function to measure</param>
        /// <param name="times">Number of function calls.</param>
        /// <param name="units">Measure units, by default Auto</param>
        /// <param name="jitterTimes">Number of jitter calls, 5 by default.</param>
        /// <param name="begin">Function call before each measured function.</param>
        /// <param name="end">Function call after each measured function.</param>
        /// <returns>Return the measure metrics.</returns>
        public static Metrics Measure(
            this Action action,
            int times = DEFAULT_TIMES, Units units = DEFAULT_UNITS, int jitterTimes = DEFAULT_JITTER_TIMES, 
            Action begin = null, 
            Action end = null)
        {
            for (var i = 0; i < jitterTimes; i++)
            {
	            begin?.Invoke();
	            action();
	            end?.Invoke();
            }

            var metrics = new double[times];
            var sw = new Stopwatch();
            for (var i = 0; i < times; i++)
            {
				begin?.Invoke();

				sw.Start();
                action();
                sw.Stop();
                metrics[i] = sw.Elapsed.TotalMilliseconds;
                sw.Reset();

				end?.Invoke();
			}

            return GenerateMetrics(metrics, units);
        }

        /// <summary>
        /// Measure a function by running it many times. By default 5 jitter run which will be ignore in the metrics then run 100 times.
        /// </summary>
        /// <param name="action">Function to measure</param>
        /// <param name="arg">Function argument</param>
        /// <param name="times">Number of function calls.</param>
        /// <param name="units">Measure units, by default Auto</param>
        /// <param name="jitterTimes">Number of jitter calls, 5 by default.</param>
        /// <param name="begin">Function call before each measured function.</param>
        /// <param name="end">Function call after each measured function.</param>
        /// <returns>Return the measure metrics.</returns>
        public static Metrics Measure<T>(
            this Action<T> action, 
            T arg,
            int times = DEFAULT_TIMES, Units units = DEFAULT_UNITS, int jitterTimes = DEFAULT_JITTER_TIMES, 
            Action<T> begin = null,
            Action<T> end = null)
        {
            for (var i = 0; i < jitterTimes; i++)
            {
	            begin?.Invoke(arg);
	            action(arg);
	            end?.Invoke(arg);
            }

            var metrics = new double[times];
            var sw = new Stopwatch();
            for (var i = 0; i < times; i++)
            {
	            begin?.Invoke(arg);

	            sw.Start();
                action(arg);
                sw.Stop();
                metrics[i] = sw.Elapsed.TotalMilliseconds;
                sw.Reset();

	            end?.Invoke(arg);
            }

            return GenerateMetrics(metrics, units);
        }

        /// <summary>
        /// Measure a function by running it many times. By default 5 jitter run which will be ignore in the metrics then run 100 times.
        /// </summary>
        /// <param name="action">Function to measure</param>
        /// <param name="arg1">Function argument 1</param>
        /// <param name="arg2">Function argument 2</param>
        /// <param name="times">Number of function calls.</param>
        /// <param name="units">Measure units, by default Auto</param>
        /// <param name="jitterTimes">Number of jitter calls, 5 by default.</param>
        /// <param name="begin">Function call before each measured function.</param>
        /// <param name="end">Function call after each measured function.</param>
        /// <returns>Return the measure metrics.</returns>
        public static Metrics Measure<T1, T2>(
            this Action<T1, T2> action, T1 arg1, T2 arg2,
            int times = DEFAULT_TIMES, Units units = DEFAULT_UNITS, int jitterTimes = DEFAULT_JITTER_TIMES, 
            Action<T1, T2> begin = null,
            Action<T1, T2> end = null)
        {
            for (var i = 0; i < jitterTimes; i++)
            {
	            begin?.Invoke(arg1, arg2);
	            action(arg1, arg2);
	            end?.Invoke(arg1, arg2);
            }

            var metrics = new double[times];
            var sw = new Stopwatch();
            for (var i = 0; i < times; i++)
            {
	            begin?.Invoke(arg1, arg2);

	            sw.Start();
                action(arg1, arg2);
                sw.Stop();
                metrics[i] = sw.Elapsed.TotalMilliseconds;
                sw.Reset();

	            end?.Invoke(arg1, arg2);
            }

            return GenerateMetrics(metrics, units);
        }

        /// <summary>
        /// Measure a function by running it many times. By default 5 jitter run which will be ignore in the metrics then run 100 times.
        /// </summary>
        /// <param name="action">Function to measure</param>
        /// <param name="arg1">Function argument 1</param>
        /// <param name="arg2">Function argument 2</param>
        /// <param name="arg3">Function argument 3</param>
        /// <param name="times">Number of function calls.</param>
        /// <param name="units">Measure units, by default Auto</param>
        /// <param name="jitterTimes">Number of jitter calls, 5 by default.</param>
        /// <param name="begin">Function call before each measured function.</param>
        /// <param name="end">Function call after each measured function.</param>
        /// <returns>Return the measure metrics.</returns>
        public static Metrics Measure<T1, T2, T3>(
            this Action<T1, T2, T3> action, 
            T1 arg1, T2 arg2, T3 arg3,
            int times = DEFAULT_TIMES, Units units = DEFAULT_UNITS, int jitterTimes = DEFAULT_JITTER_TIMES, 
            Action<T1, T2, T3> begin = null,
            Action<T1, T2, T3> end = null)
        {
            for (var i = 0; i < jitterTimes; i++)
            {
				begin?.Invoke(arg1, arg2, arg3);

				action(arg1, arg2, arg3);

				end?.Invoke(arg1, arg2, arg3);
			}

            var metrics = new double[times];
            var sw = new Stopwatch();
            for (var i = 0; i < times; i++)
            {
				begin?.Invoke(arg1, arg2, arg3);

				sw.Start();
                action(arg1, arg2, arg3);
                sw.Stop();
                metrics[i] = sw.Elapsed.TotalMilliseconds;
                sw.Reset();

				end?.Invoke(arg1, arg2, arg3);
			}

            return GenerateMetrics(metrics, units);
        }

        /// <summary>
        /// Measure a function by running it many times. By default 5 jitter run which will be ignore in the metrics then run 100 times.
        /// </summary>
        /// <param name="action">Function to measure</param>
        /// <param name="arg1">Function argument 1</param>
        /// <param name="arg2">Function argument 2</param>
        /// <param name="arg3">Function argument 3</param>
        /// <param name="arg4">Function argument 4</param>
        /// <param name="times">Number of function calls.</param>
        /// <param name="units">Measure units, by default Auto</param>
        /// <param name="jitterTimes">Number of jitter calls, 5 by default.</param>
        /// <param name="begin">Function call before each measured function.</param>
        /// <param name="end">Function call after each measured function.</param>
        /// <returns>Return the measure metrics.</returns>
        public static Metrics Measure<T1, T2, T3, T4>(
            this Action<T1, T2, T3, T4> action, 
            T1 arg1, T2 arg2, T3 arg3, T4 arg4,
            int times = DEFAULT_TIMES, Units units = DEFAULT_UNITS, int jitterTimes = DEFAULT_JITTER_TIMES, 
            Action<T1, T2, T3, T4> begin = null,
            Action<T1, T2, T3, T4> end = null)
        {
            for (var i = 0; i < jitterTimes; i++)
            {
				begin?.Invoke(arg1, arg2, arg3, arg4);

				action(arg1, arg2, arg3, arg4);

				end?.Invoke(arg1, arg2, arg3, arg4);
			}

            var metrics = new double[times];
            var sw = new Stopwatch();
            for (var i = 0; i < times; i++)
            {
				begin?.Invoke(arg1, arg2, arg3, arg4);

				sw.Start();
                action(arg1, arg2, arg3, arg4);
                sw.Stop();
                metrics[i] = sw.Elapsed.TotalMilliseconds;
                sw.Reset();

				end?.Invoke(arg1, arg2, arg3, arg4);
			}

            return GenerateMetrics(metrics, units);
        }

        /// <summary>
        /// Measure a function by running it many times. By default 5 jitter run which will be ignore in the metrics then run 100 times.
        /// </summary>
        /// <param name="action">Function to measure</param>
        /// <param name="arg1">Function argument 1</param>
        /// <param name="arg2">Function argument 2</param>
        /// <param name="arg3">Function argument 3</param>
        /// <param name="arg4">Function argument 4</param>
        /// <param name="arg5">Function argument 5</param>
        /// <param name="times">Number of function calls.</param>
        /// <param name="units">Measure units, by default Auto</param>
        /// <param name="jitterTimes">Number of jitter calls, 5 by default.</param>
        /// <param name="begin">Function call before each measured function.</param>
        /// <param name="end">Function call after each measured function.</param>
        /// <returns>Return the measure metrics.</returns>
        public static Metrics Measure<T1, T2, T3, T4, T5>(
            this Action<T1, T2, T3, T4, T5> action, 
            T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5,
            int times = DEFAULT_TIMES, Units units = DEFAULT_UNITS, int jitterTimes = DEFAULT_JITTER_TIMES,
            Action<T1, T2, T3, T4, T5> begin = null,
            Action<T1, T2, T3, T4, T5> end = null)
        {
            for (var i = 0; i < jitterTimes; i++)
            {
				begin?.Invoke(arg1, arg2, arg3, arg4, arg5);

				action(arg1, arg2, arg3, arg4, arg5);

				end?.Invoke(arg1, arg2, arg3, arg4, arg5);
			}

            var metrics = new double[times];
            var sw = new Stopwatch();
            for (var i = 0; i < times; i++)
            {
				begin?.Invoke(arg1, arg2, arg3, arg4, arg5);

				sw.Start();
                action(arg1, arg2, arg3, arg4, arg5);
                sw.Stop();
                metrics[i] = sw.Elapsed.TotalMilliseconds;
                sw.Reset();

				end?.Invoke(arg1, arg2, arg3, arg4, arg5);
			}

            return GenerateMetrics(metrics, units);
        }

        /// <summary>
        /// Measure a function by running it many times. By default 5 jitter run which will be ignore in the metrics then run 100 times.
        /// </summary>
        /// <param name="action">Function to measure</param>
        /// <param name="arg1">Function argument 1</param>
        /// <param name="arg2">Function argument 2</param>
        /// <param name="arg3">Function argument 3</param>
        /// <param name="arg4">Function argument 4</param>
        /// <param name="arg5">Function argument 5</param>
        /// <param name="arg6">Function argument 6</param>
        /// <param name="times">Number of function calls.</param>
        /// <param name="units">Measure units, by default Auto</param>
        /// <param name="jitterTimes">Number of jitter calls, 5 by default.</param>
        /// <param name="begin">Function call before each measured function.</param>
        /// <param name="end">Function call after each measured function.</param>
        /// <returns>Return the measure metrics.</returns>
        public static Metrics Measure<T1, T2, T3, T4, T5, T6>(
            this Action<T1, T2, T3, T4, T5, T6> action, 
            T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6,
            int times = DEFAULT_TIMES, Units units = DEFAULT_UNITS, int jitterTimes = DEFAULT_JITTER_TIMES,
            Action<T1, T2, T3, T4, T5, T6> begin = null,
            Action<T1, T2, T3, T4, T5, T6> end = null)
        {
            for (var i = 0; i < jitterTimes; i++)
            {
				begin?.Invoke(arg1, arg2, arg3, arg4, arg5, arg6);

				action(arg1, arg2, arg3, arg4, arg5, arg6);

				end?.Invoke(arg1, arg2, arg3, arg4, arg5, arg6);
			}

            var metrics = new double[times];
            var sw = new Stopwatch();
            for (var i = 0; i < times; i++)
            {
				begin?.Invoke(arg1, arg2, arg3, arg4, arg5, arg6);

				sw.Start();
                action(arg1, arg2, arg3, arg4, arg5, arg6);
                sw.Stop();
                metrics[i] = sw.Elapsed.TotalMilliseconds;
                sw.Reset();

				end?.Invoke(arg1, arg2, arg3, arg4, arg5, arg6);
			}

            return GenerateMetrics(metrics, units);
        }

        /// <summary>
        /// Measure a function by running it many times. By default 5 jitter run which will be ignore in the metrics then run 100 times.
        /// </summary>
        /// <param name="action">Function to measure</param>
        /// <param name="arg1">Function argument 1</param>
        /// <param name="arg2">Function argument 2</param>
        /// <param name="arg3">Function argument 3</param>
        /// <param name="arg4">Function argument 4</param>
        /// <param name="arg5">Function argument 5</param>
        /// <param name="arg6">Function argument 6</param>
        /// <param name="arg7">Function argument 7</param>
        /// <param name="times">Number of function calls.</param>
        /// <param name="units">Measure units, by default Auto</param>
        /// <param name="jitterTimes">Number of jitter calls, 5 by default.</param>
        /// <param name="begin">Function call before each measured function.</param>
        /// <param name="end">Function call after each measured function.</param>
        /// <returns>Return the measure metrics.</returns>
        public static Metrics Measure<T1, T2, T3, T4, T5, T6, T7>(
            this Action<T1, T2, T3, T4, T5, T6, T7> action, 
            T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7,
            int times = DEFAULT_TIMES, Units units = DEFAULT_UNITS, int jitterTimes = DEFAULT_JITTER_TIMES,
            Action<T1, T2, T3, T4, T5, T6, T7> begin = null,
            Action<T1, T2, T3, T4, T5, T6, T7> end = null)
        {
            for (var i = 0; i < jitterTimes; i++)
            {
				begin?.Invoke(arg1, arg2, arg3, arg4, arg5, arg6, arg7);

				action(arg1, arg2, arg3, arg4, arg5, arg6, arg7);

				end?.Invoke(arg1, arg2, arg3, arg4, arg5, arg6, arg7);
			}

            var metrics = new double[times];
            var sw = new Stopwatch();
            for (var i = 0; i < times; i++)
            {
				begin?.Invoke(arg1, arg2, arg3, arg4, arg5, arg6, arg7);

				sw.Start();
                action(arg1, arg2, arg3, arg4, arg5, arg6, arg7);
                sw.Stop();
                metrics[i] = sw.Elapsed.TotalMilliseconds;
                sw.Reset();

				end?.Invoke(arg1, arg2, arg3, arg4, arg5, arg6, arg7);
			}

            return GenerateMetrics(metrics, units);
        }

        /// <summary>
        /// Measure a function by running it many times. By default 5 jitter run which will be ignore in the metrics then run 100 times.
        /// </summary>
        /// <param name="action">Function to measure</param>
        /// <param name="arg1">Function argument 1</param>
        /// <param name="arg2">Function argument 2</param>
        /// <param name="arg3">Function argument 3</param>
        /// <param name="arg4">Function argument 4</param>
        /// <param name="arg5">Function argument 5</param>
        /// <param name="arg6">Function argument 6</param>
        /// <param name="arg7">Function argument 7</param>
        /// <param name="arg8">Function argument 8</param>
        /// <param name="times">Number of function calls.</param>
        /// <param name="units">Measure units, by default Auto</param>
        /// <param name="jitterTimes">Number of jitter calls, 5 by default.</param>
        /// <param name="begin">Function call before each measured function.</param>
        /// <param name="end">Function call after each measured function.</param>
        /// <returns>Return the measure metrics.</returns>
        public static Metrics Measure<T1, T2, T3, T4, T5, T6, T7, T8>(
            this Action<T1, T2, T3, T4, T5, T6, T7, T8> action, 
            T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8,
            int times = DEFAULT_TIMES, Units units = DEFAULT_UNITS, int jitterTimes = DEFAULT_JITTER_TIMES,
            Action<T1, T2, T3, T4, T5, T6, T7, T8> begin = null,
            Action<T1, T2, T3, T4, T5, T6, T7, T8> end = null)
        {
            for (var i = 0; i < jitterTimes; i++)
            {
				begin?.Invoke(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);

				action(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);

				end?.Invoke(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
			}

            var metrics = new double[times];
            var sw = new Stopwatch();
            for (var i = 0; i < times; i++)
            {
				begin?.Invoke(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);

				sw.Start();
                action(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
                sw.Stop();
                metrics[i] = sw.Elapsed.TotalMilliseconds;
                sw.Reset();

				end?.Invoke(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
			}

            return GenerateMetrics(metrics, units);
        }

        /// <summary>
        /// Measure a function by running it many times. By default 5 jitter run which will be ignore in the metrics then run 100 times.
        /// </summary>
        /// <param name="action">Function to measure</param>
        /// <param name="times">Number of function calls.</param>
        /// <param name="units">Measure units, by default Auto</param>
        /// <param name="jitterTimes">Number of jitter calls, 5 by default.</param>
        /// <param name="begin">Function call before each measured function.</param>
        /// <param name="end">Function call after each measured function.</param>
        /// <returns>Return the measure metrics.</returns>
        public static Metrics Measure<TResult>(
            this Func<TResult> action,
            int times = DEFAULT_TIMES, Units units = DEFAULT_UNITS, int jitterTimes = DEFAULT_JITTER_TIMES, 
            Action begin = null, 
            Action end = null)
        {
            for (var i = 0; i < jitterTimes; i++)
            {
				begin?.Invoke();
				action();
				end?.Invoke();
			}

            var metrics = new double[times];
            var sw = new Stopwatch();
            for (var i = 0; i < times; i++)
            {
				begin?.Invoke();

				sw.Start();
                action();
                sw.Stop();
                metrics[i] = sw.Elapsed.TotalMilliseconds;
                sw.Reset();

				end?.Invoke();
			}

            return GenerateMetrics(metrics, units);
        }

        /// <summary>
        /// Measure a function by running it many times. By default 5 jitter run which will be ignore in the metrics then run 100 times.
        /// </summary>
        /// <param name="action">Function to measure</param>
        /// <param name="arg">Function argument</param>
        /// <param name="times">Number of function calls.</param>
        /// <param name="units">Measure units, by default Auto</param>
        /// <param name="jitterTimes">Number of jitter calls, 5 by default.</param>
        /// <param name="begin">Function call before each measured function.</param>
        /// <param name="end">Function call after each measured function.</param>
        /// <returns>Return the measure metrics.</returns>
        public static Metrics Measure<T, TResult>(
            this Func<T, TResult> action, T arg,
            int times = DEFAULT_TIMES, Units units = DEFAULT_UNITS, int jitterTimes = DEFAULT_JITTER_TIMES,
            Action<T> begin = null,
            Action<T> end = null)
        {
            for (var i = 0; i < jitterTimes; i++)
            {
				begin?.Invoke(arg);
				action(arg);
				end?.Invoke(arg);
			}

            var metrics = new double[times];
            var sw = new Stopwatch();
            for (var i = 0; i < times; i++)
            {
				begin?.Invoke(arg);

				sw.Start();
                action(arg);
                sw.Stop();
                metrics[i] = sw.Elapsed.TotalMilliseconds;
                sw.Reset();

				end?.Invoke(arg);
			}

            return GenerateMetrics(metrics, units);
        }

        /// <summary>
        /// Measure a function by running it many times. By default 5 jitter run which will be ignore in the metrics then run 100 times.
        /// </summary>
        /// <param name="action">Function to measure</param>
        /// <param name="arg1">Function argument 1</param>
        /// <param name="arg2">Function argument 2</param>
        /// <param name="times">Number of function calls.</param>
        /// <param name="units">Measure units, by default Auto</param>
        /// <param name="jitterTimes">Number of jitter calls, 5 by default.</param>
        /// <param name="begin">Function call before each measured function.</param>
        /// <param name="end">Function call after each measured function.</param>
        /// <returns>Return the measure metrics.</returns>
        public static Metrics Measure<T1, T2, TResult>(
            this Func<T1, T2, TResult> action, 
            T1 arg1, T2 arg2,
            int times = DEFAULT_TIMES, Units units = DEFAULT_UNITS, int jitterTimes = DEFAULT_JITTER_TIMES,
            Action<T1, T2> begin = null,
            Action<T1, T2> end = null)
        {
            for (var i = 0; i < jitterTimes; i++)
            {
				begin?.Invoke(arg1, arg2);
				action(arg1, arg2);
				end?.Invoke(arg1, arg2);
			}

            var metrics = new double[times];
            var sw = new Stopwatch();
            for (var i = 0; i < times; i++)
            {
				begin?.Invoke(arg1, arg2);

				sw.Start();
                action(arg1, arg2);
                sw.Stop();
                metrics[i] = sw.Elapsed.TotalMilliseconds;
                sw.Reset();

				end?.Invoke(arg1, arg2);
			}

            return GenerateMetrics(metrics, units);
        }

        /// <summary>
        /// Measure a function by running it many times. By default 5 jitter run which will be ignore in the metrics then run 100 times.
        /// </summary>
        /// <param name="action">Function to measure</param>
        /// <param name="arg1">Function argument 1</param>
        /// <param name="arg2">Function argument 2</param>
        /// <param name="arg3">Function argument 3</param>
        /// <param name="times">Number of function calls.</param>
        /// <param name="units">Measure units, by default Auto</param>
        /// <param name="jitterTimes">Number of jitter calls, 5 by default.</param>
        /// <param name="begin">Function call before each measured function.</param>
        /// <param name="end">Function call after each measured function.</param>
        /// <returns>Return the measure metrics.</returns>
        public static Metrics Measure<T1, T2, T3, TResult>(
            this Func<T1, T2, T3, TResult> action, 
            T1 arg1, T2 arg2, T3 arg3,
            int times = DEFAULT_TIMES, Units units = DEFAULT_UNITS, int jitterTimes = DEFAULT_JITTER_TIMES,
            Action<T1, T2, T3> begin = null,
            Action<T1, T2, T3> end = null)
        {
            for (var i = 0; i < jitterTimes; i++)
            {
				begin?.Invoke(arg1, arg2, arg3);

				action(arg1, arg2, arg3);

				end?.Invoke(arg1, arg2, arg3);
			}

            var metrics = new double[times];
            var sw = new Stopwatch();
            for (var i = 0; i < times; i++)
            {
				begin?.Invoke(arg1, arg2, arg3);

				sw.Start();
                action(arg1, arg2, arg3);
                sw.Stop();
                metrics[i] = sw.Elapsed.TotalMilliseconds;
                sw.Reset();

				end?.Invoke(arg1, arg2, arg3);
			}

            return GenerateMetrics(metrics, units);
        }

        /// <summary>
        /// Measure a function by running it many times. By default 5 jitter run which will be ignore in the metrics then run 100 times.
        /// </summary>
        /// <param name="action">Function to measure</param>
        /// <param name="arg1">Function argument 1</param>
        /// <param name="arg2">Function argument 2</param>
        /// <param name="arg3">Function argument 3</param>
        /// <param name="arg4">Function argument 4</param>
        /// <param name="times">Number of function calls.</param>
        /// <param name="units">Measure units, by default Auto</param>
        /// <param name="jitterTimes">Number of jitter calls, 5 by default.</param>
        /// <param name="begin">Function call before each measured function.</param>
        /// <param name="end">Function call after each measured function.</param>
        /// <returns>Return the measure metrics.</returns>
        public static Metrics Measure<T1, T2, T3, T4, TResult>(
            this Func<T1, T2, T3, T4, TResult> action, 
            T1 arg1, T2 arg2, T3 arg3, T4 arg4,
            int times = DEFAULT_TIMES, Units units = DEFAULT_UNITS, int jitterTimes = DEFAULT_JITTER_TIMES, 
            Action<T1, T2, T3, T4> begin = null,
            Action<T1, T2, T3, T4> end = null)
        {
            for (var i = 0; i < jitterTimes; i++)
            {
				begin?.Invoke(arg1, arg2, arg3, arg4);

				action(arg1, arg2, arg3, arg4);

				end?.Invoke(arg1, arg2, arg3, arg4);
			}

            var metrics = new double[times];
            var sw = new Stopwatch();
            for (var i = 0; i < times; i++)
            {
				begin?.Invoke(arg1, arg2, arg3, arg4);

				sw.Start();
                action(arg1, arg2, arg3, arg4);
                sw.Stop();
                metrics[i] = sw.Elapsed.TotalMilliseconds;
                sw.Reset();

				end?.Invoke(arg1, arg2, arg3, arg4);
			}

            return GenerateMetrics(metrics, units);
        }

        /// <summary>
        /// Measure a function by running it many times. By default 5 jitter run which will be ignore in the metrics then run 100 times.
        /// </summary>
        /// <param name="action">Function to measure</param>
        /// <param name="arg1">Function argument 1</param>
        /// <param name="arg2">Function argument 2</param>
        /// <param name="arg3">Function argument 3</param>
        /// <param name="arg4">Function argument 4</param>
        /// <param name="arg5">Function argument 5</param>
        /// <param name="times">Number of function calls.</param>
        /// <param name="units">Measure units, by default Auto</param>
        /// <param name="jitterTimes">Number of jitter calls, 5 by default.</param>
        /// <param name="begin">Function call before each measured function.</param>
        /// <param name="end">Function call after each measured function.</param>
        /// <returns>Return the measure metrics.</returns>
        public static Metrics Measure<T1, T2, T3, T4, T5, TResult>(
            this Func<T1, T2, T3, T4, T5, TResult> action, 
            T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5,
            int times = DEFAULT_TIMES, Units units = DEFAULT_UNITS, int jitterTimes = DEFAULT_JITTER_TIMES, 
            Action<T1, T2, T3, T4, T5> begin = null,
            Action<T1, T2, T3, T4, T5> end = null)
        {
            for (var i = 0; i < jitterTimes; i++)
            {
				begin?.Invoke(arg1, arg2, arg3, arg4, arg5);

				action(arg1, arg2, arg3, arg4, arg5);

				end?.Invoke(arg1, arg2, arg3, arg4, arg5);
			}

            var metrics = new double[times];
            var sw = new Stopwatch();
            for (var i = 0; i < times; i++)
            {
				begin?.Invoke(arg1, arg2, arg3, arg4, arg5);

				sw.Start();
                action(arg1, arg2, arg3, arg4, arg5);
                sw.Stop();
                metrics[i] = sw.Elapsed.TotalMilliseconds;
                sw.Reset();

				end?.Invoke(arg1, arg2, arg3, arg4, arg5);
			}

            return GenerateMetrics(metrics, units);
        }

        /// <summary>
        /// Measure a function by running it many times. By default 5 jitter run which will be ignore in the metrics then run 100 times.
        /// </summary>
        /// <param name="action">Function to measure</param>
        /// <param name="arg1">Function argument 1</param>
        /// <param name="arg2">Function argument 2</param>
        /// <param name="arg3">Function argument 3</param>
        /// <param name="arg4">Function argument 4</param>
        /// <param name="arg5">Function argument 5</param>
        /// <param name="arg6">Function argument 6</param>
        /// <param name="times">Number of function calls.</param>
        /// <param name="units">Measure units, by default Auto</param>
        /// <param name="jitterTimes">Number of jitter calls, 5 by default.</param>
        /// <param name="begin">Function call before each measured function.</param>
        /// <param name="end">Function call after each measured function.</param>
        /// <returns>Return the measure metrics.</returns>
        public static Metrics Measure<T1, T2, T3, T4, T5, T6, TResult>(
            this Func<T1, T2, T3, T4, T5, T6, TResult> action, 
            T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6,
            int times = DEFAULT_TIMES, Units units = DEFAULT_UNITS, int jitterTimes = DEFAULT_JITTER_TIMES, 
            Action<T1, T2, T3, T4, T5, T6> begin = null, 
            Action<T1, T2, T3, T4, T5, T6> end = null)
        {
            for (var i = 0; i < jitterTimes; i++)
            {
				begin?.Invoke(arg1, arg2, arg3, arg4, arg5, arg6);

				action(arg1, arg2, arg3, arg4, arg5, arg6);

				end?.Invoke(arg1, arg2, arg3, arg4, arg5, arg6);
			}

            var metrics = new double[times];
            var sw = new Stopwatch();
            for (var i = 0; i < times; i++)
            {
				begin?.Invoke(arg1, arg2, arg3, arg4, arg5, arg6);

				sw.Start();
                action(arg1, arg2, arg3, arg4, arg5, arg6);
                sw.Stop();
                metrics[i] = sw.Elapsed.TotalMilliseconds;
                sw.Reset();

				end?.Invoke(arg1, arg2, arg3, arg4, arg5, arg6);
			}

            return GenerateMetrics(metrics, units);
        }

        /// <summary>
        /// Measure a function by running it many times. By default 5 jitter run which will be ignore in the metrics then run 100 times.
        /// </summary>
        /// <param name="action">Function to measure</param>
        /// <param name="arg1">Function argument 1</param>
        /// <param name="arg2">Function argument 2</param>
        /// <param name="arg3">Function argument 3</param>
        /// <param name="arg4">Function argument 4</param>
        /// <param name="arg5">Function argument 5</param>
        /// <param name="arg6">Function argument 6</param>
        /// <param name="arg7">Function argument 7</param>
        /// <param name="times">Number of function calls.</param>
        /// <param name="units">Measure units, by default Auto</param>
        /// <param name="jitterTimes">Number of jitter calls, 5 by default.</param>
        /// <param name="begin">Function call before each measured function.</param>
        /// <param name="end">Function call after each measured function.</param>
        /// <returns>Return the measure metrics.</returns>
        public static Metrics Measure<T1, T2, T3, T4, T5, T6, T7, TResult>(
            this Func<T1, T2, T3, T4, T5, T6, T7, TResult> action, 
            T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7,
            int times = DEFAULT_TIMES, Units units = DEFAULT_UNITS, int jitterTimes = DEFAULT_JITTER_TIMES,
            Action<T1, T2, T3, T4, T5, T6, T7> begin = null,
            Action<T1, T2, T3, T4, T5, T6, T7> end = null)
        {
            for (var i = 0; i < jitterTimes; i++)
            {
				begin?.Invoke(arg1, arg2, arg3, arg4, arg5, arg6, arg7);

				action(arg1, arg2, arg3, arg4, arg5, arg6, arg7);

				end?.Invoke(arg1, arg2, arg3, arg4, arg5, arg6, arg7);
			}

            var metrics = new double[times];
            var sw = new Stopwatch();
            for (var i = 0; i < times; i++)
            {
				begin?.Invoke(arg1, arg2, arg3, arg4, arg5, arg6, arg7);

				sw.Start();
                action(arg1, arg2, arg3, arg4, arg5, arg6, arg7);
                sw.Stop();
                metrics[i] = sw.Elapsed.TotalMilliseconds;
                sw.Reset();

				end?.Invoke(arg1, arg2, arg3, arg4, arg5, arg6, arg7);
			}

            return GenerateMetrics(metrics, units);
        }

        /// <summary>
        /// Measure a function by running it many times. By default 5 jitter run which will be ignore in the metrics then run 100 times.
        /// </summary>
        /// <param name="action">Function to measure</param>
        /// <param name="arg1">Function argument 1</param>
        /// <param name="arg2">Function argument 2</param>
        /// <param name="arg3">Function argument 3</param>
        /// <param name="arg4">Function argument 4</param>
        /// <param name="arg5">Function argument 5</param>
        /// <param name="arg6">Function argument 6</param>
        /// <param name="arg7">Function argument 7</param>
        /// <param name="arg8">Function argument 8</param>
        /// <param name="times">Number of function calls.</param>
        /// <param name="units">Measure units, by default Auto</param>
        /// <param name="jitterTimes">Number of jitter calls, 5 by default.</param>
        /// <param name="begin">Function call before each measured function.</param>
        /// <param name="end">Function call after each measured function.</param>
        /// <returns>Return the measure metrics.</returns>
        public static Metrics Measure<T1, T2, T3, T4, T5, T6, T7, T8, TResult>(
            this Func<T1, T2, T3, T4, T5, T6, T7, T8, TResult> action, 
            T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8,
            int times = DEFAULT_TIMES, Units units = DEFAULT_UNITS, int jitterTimes = DEFAULT_JITTER_TIMES, 
            Action<T1, T2, T3, T4, T5, T6, T7, T8> begin = null, 
            Action<T1, T2, T3, T4, T5, T6, T7, T8> end = null)
        {
            for (var i = 0; i < jitterTimes; i++)
            {
				begin?.Invoke(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);

				action(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);

				end?.Invoke(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
			}

            var metrics = new double[times];
            var sw = new Stopwatch();
            for (var i = 0; i < times; i++)
            {
				begin?.Invoke(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);

				sw.Start();
                action(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
                sw.Stop();
                metrics[i] = sw.Elapsed.TotalMilliseconds;
                sw.Reset();

				end?.Invoke(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
			}

            return GenerateMetrics(metrics, units);
        }

        #region Helpers

        private static Metrics GenerateMetrics(double[] metrics, Units units)
        {
            return new Metrics(metrics, units);
        }

        private static Units GetUnits(double ms)
        {
            if (ms < 1e-3) return Units.Nanos;
            if (ms < 1) return Units.Micros;
            if (ms < 1e3) return Units.Millis;
            if (ms < 1e3 * 60) return Units.Secs;
            return ms < 1e3 * 60 * 60 ? Units.Mins : Units.Hours;
        }

        private static double GetUnitFactor(Units units)
        {
            switch (units)
            {
                case Units.Nanos:
                    return 1e6;
                case Units.Micros:
                    return 1e3;
                case Units.Secs:
                    return 1e-3;
                case Units.Mins:
                    return 6e-4;
                case Units.Hours:
                    return 36e-5;
                default:
                    return 1;
            }
        }

        #endregion

        #region Nested Types

        /// <summary>
        /// Defines the metric unit measure.
        /// </summary>
        public enum Units
        {
            Auto,
            Nanos,
            Micros,
            Millis,
            Secs,
            Mins,
            Hours,
        }

        /// <summary>
        /// Measure metrics 
        /// </summary>
        public class Metrics
        {
            private readonly double _min;
            private readonly double _mean;
            private readonly double _median;
            private readonly double _max;
	        private readonly Units _units;

            /// <summary>
            /// Gets the min measured value.
            /// </summary>
            public double Min => _min;

	        /// <summary>
            /// Gets the mean measured value.
            /// </summary>
            public double Mean => _mean;

	        /// <summary>
            /// Gets the median measured value.
            /// </summary>
            public double Median => _median;

	        /// <summary>
            /// Gets the max measured value.
            /// </summary>
            public double Max => _max;

	        /// <summary>
            /// Gets the units of measured value.
            /// </summary>
            public Units Units => _units;

	        /// <summary>
            /// Gets all measures.
            /// </summary>
            public double[] Measures { get; }

	        /// <summary>
            /// Ctor.
            /// </summary>
            /// <param name="measures">All measures in milliseconds.</param>
            /// <param name="units">Measure unit to compute.</param>
            public Metrics(double[] measures, Units units)
            {
                var ordered = measures.OrderBy(p => p).ToArray();
                if (ordered.Length == 0) return;

                var length = ordered.Length;
				_min = ordered[0];
				_max = ordered[length - 1];
                
                var sum = 0.0;
                var medianIdx = length / 2;
                for (var i = 0; i < length; i++)
                {
					_min = Math.Min(_min, measures[i]);
					_max = Math.Max(_max, measures[i]);
                    sum += measures[i];
                    if (i == medianIdx)
						_median = measures[i];
                }
				_mean = sum / length;

                if (units == Units.Auto)
                    units = GetUnits(_mean);
                var factor = GetUnitFactor(units);

				_min *= factor;
				_mean *= factor;
				_median *= factor;
				_max *= factor;

				Measures = measures;
				_units = units;
            }

            public override string ToString()
            {
                return $"Min: {_min}, Mean: {_mean}, Median: {_median}, Max: {_max}, Units: {_units}";
            }
        }

        #endregion
    }
}
