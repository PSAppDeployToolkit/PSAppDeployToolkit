using System;
using System.Threading.Tasks;
using System.Management.Automation;

namespace PSADT.Shared
{
    /// <summary>
    /// A wrapper class that allows the invocation of a <see cref="ScriptBlock"/> with either an <see cref="Action{T}"/> delegate or asynchronous <see cref="Func{Task}"/> delegates.
    /// </summary>
    public class ScriptBlockWrapper
    {
        private Action<byte[]>? _originalAction;
        private Func<Task>? _asyncAction;
        private Func<Task<object>>? _asyncFunc;
        private ScriptBlock _scriptBlock;

        /// <summary>
        /// Initializes a new instance of the <see cref="ScriptBlockWrapper"/> class with an action delegate and a <see cref="ScriptBlock"/>.
        /// </summary>
        /// <param name="originalAction">The original <see cref="Action{T}"/> delegate to wrap.</param>
        /// <param name="scriptBlock">The <see cref="ScriptBlock"/> to invoke.</param>
        public ScriptBlockWrapper(Action<byte[]> originalAction, ScriptBlock scriptBlock)
        {
            _originalAction = originalAction ?? throw new ArgumentNullException(nameof(originalAction));
            _scriptBlock = scriptBlock ?? throw new ArgumentNullException(nameof(scriptBlock));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ScriptBlockWrapper"/> class with an asynchronous function delegate and a <see cref="ScriptBlock"/>.
        /// </summary>
        /// <param name="asyncAction">The asynchronous <see cref="Func{Task}"/> delegate to execute.</param>
        /// <param name="scriptBlock">The <see cref="ScriptBlock"/> to invoke after the asynchronous function completes.</param>
        public ScriptBlockWrapper(Func<Task> asyncAction, ScriptBlock scriptBlock)
        {
            _asyncAction = asyncAction ?? throw new ArgumentNullException(nameof(asyncAction));
            _scriptBlock = scriptBlock ?? throw new ArgumentNullException(nameof(scriptBlock));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ScriptBlockWrapper"/> class with an asynchronous function delegate that returns a result and a <see cref="ScriptBlock"/>.
        /// </summary>
        /// <param name="asyncFunc">The asynchronous <see cref="Func{Task{T}}"/> delegate to execute, returning a result that is passed to the <see cref="ScriptBlock"/>.</param>
        /// <param name="scriptBlock">The <see cref="ScriptBlock"/> to invoke after the asynchronous function completes.</param>
        public ScriptBlockWrapper(Func<Task<object>> asyncFunc, ScriptBlock scriptBlock)
        {
            _asyncFunc = asyncFunc ?? throw new ArgumentNullException(nameof(asyncFunc));
            _scriptBlock = scriptBlock ?? throw new ArgumentNullException(nameof(scriptBlock));
        }

        /// <summary>
        /// Wraps the original <see cref="Action{T}"/> delegate and returns a new delegate that invokes the <see cref="ScriptBlock"/>.
        /// </summary>
        /// <returns>An <see cref="Action{T}"/> delegate that invokes the <see cref="ScriptBlock"/>.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the original action is not available.</exception>
        public Action<byte[]> GetWrappedActionDelegate()
        {
            if (_originalAction == null)
            {
                throw new InvalidOperationException("Original action is not available.");
            }

            // Wrap the original action to invoke the ScriptBlock
            return bytes => _scriptBlock.Invoke(bytes);
        }

        /// <summary>
        /// Wraps the asynchronous action and returns a new <see cref="Func{Task}"/> that invokes the <see cref="ScriptBlock"/> after the asynchronous function completes.
        /// </summary>
        /// <returns>A wrapped <see cref="Func{Task}"/> delegate.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the asynchronous action is not available.</exception>
        public Func<Task> GetWrappedAsyncAction()
        {
            if (_asyncAction == null)
            {
                throw new InvalidOperationException("Asynchronous action is not available.");
            }

            // Wrap the async action to invoke the ScriptBlock
            return async () =>
            {
                await _asyncAction().ConfigureAwait(false);
                _scriptBlock.Invoke();
            };
        }

        /// <summary>
        /// Wraps the asynchronous function and returns a new <see cref="Func{Task{T}}"/> that invokes the <see cref="ScriptBlock"/> after the asynchronous function completes.
        /// </summary>
        /// <typeparam name="T">The type of the result returned by the asynchronous function.</typeparam>
        /// <returns>A wrapped <see cref="Func{Task{T}}"/> delegate.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the asynchronous function is not available.</exception>
        public Func<Task<T>> GetWrappedAsyncFunc<T>()
        {
            if (_asyncFunc == null)
            {
                throw new InvalidOperationException("Asynchronous function is not available.");
            }

            // Wrap the async function to invoke the ScriptBlock with the result
            return async () =>
            {
                T result = (T)(await _asyncFunc().ConfigureAwait(false));  // Cast the result to T
                _scriptBlock.Invoke(result);
                return result;
            };
        }

        /// <summary>
        /// Returns the original <see cref="Action{T}"/> delegate provided to the constructor.
        /// </summary>
        /// <returns>The original <see cref="Action{T}"/> delegate.</returns>
        public Action<byte[]>? GetOriginalActionDelegate()
        {
            return _originalAction;
        }

        /// <summary>
        /// Returns the original asynchronous action delegate provided to the constructor.
        /// </summary>
        /// <returns>The original <see cref="Func{Task}"/> delegate.</returns>
        public Func<Task>? GetOriginalAsyncActionDelegate()
        {
            return _asyncAction;
        }

        /// <summary>
        /// Returns the original asynchronous function delegate provided to the constructor.
        /// </summary>
        /// <returns>The original <see cref="Func{Task{T}}"/> delegate.</returns>
        public Func<Task<object>>? GetOriginalAsyncFuncDelegate()
        {
            return _asyncFunc;
        }
    }
}



