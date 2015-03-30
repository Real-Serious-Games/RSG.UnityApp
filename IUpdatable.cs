using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RSG
{
    /// <summary>
    /// Interface to an updatable object.
    /// </summary>
    public interface IUpdatable
    {
        /// <summary>
        /// Update the object.
        /// </summary>
        void Update(float timeDelta);
    }

    /// <summary>
    /// Interface to a late updatable object.
    /// </summary>
    public interface ILateUpdatable
    {
        /// <summary>
        /// Update the object.
        /// </summary>
        void LateUpdate(float timeDelta);
    }

    /// <summary>
    /// Inteface for an object that gets an update after the end of each frame
    /// </summary>
    public interface IEndOfFrameUpdatable
    {
        /// <summary>
        /// End of frame is reached
        /// </summary>
        void EndOfFrame();
    }

    /// <summary>
    /// Interface for an object that is renderable.
    /// </summary>
    public interface IRenderable
    {
        /// <summary>
        /// Render the object.
        /// </summary>
        void Render();
    }
}
