using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace RSG.Tests
{

    public class TaskManagerTests
    {
        TaskManager testObject;

        void Init()
        {
            testObject = new TaskManager();
        }

        [Fact]
        public void registering_updateable_twice_causes_exception()
        {
            Init();

            var mockUpdateable = new Mock<IUpdatable>();

            testObject.RegisterUpdatable(mockUpdateable.Object);

            Assert.Throws<FormattedException>(() =>
                testObject.RegisterUpdatable(mockUpdateable.Object)
            );
        }

        [Fact]
        public void app_update_also_updates_registered_updateables()
        {
            Init();

            var mockUpdateable1 = new Mock<IUpdatable>();
            var mockUpdateable2 = new Mock<IUpdatable>();

            var deltaTime = 0.5f;

            testObject.RegisterUpdatable(mockUpdateable1.Object);
            testObject.RegisterUpdatable(mockUpdateable2.Object);

            testObject.Update(deltaTime);

            mockUpdateable1.Verify(m => m.Update(deltaTime), Times.Once());
            mockUpdateable2.Verify(m => m.Update(deltaTime), Times.Once());
        }

        [Fact]
        public void removing_an_updateable_means_it_is_no_longer_updated()
        {
            Init();

            var mockUpdateable = new Mock<IUpdatable>();

            var deltaTime = 0.5f;

            testObject.RegisterUpdatable(mockUpdateable.Object);
            testObject.UnregisterUpdatable(mockUpdateable.Object);

            testObject.Update(deltaTime);

            mockUpdateable.Verify(m => m.Update(deltaTime), Times.Never());
        }

        [Fact]
        public void registering_late_updateable_twice_causes_exception()
        {
            Init();

            var mockUpdateable = new Mock<ILateUpdatable>();

            testObject.RegisterLateUpdatable(mockUpdateable.Object);

            Assert.Throws<FormattedException>(() =>
                testObject.RegisterLateUpdatable(mockUpdateable.Object)
            );
        }

        [Fact]
        public void app_update_also_updates_registered_late_updateables()
        {
            Init();

            var mockUpdateable1 = new Mock<ILateUpdatable>();
            var mockUpdateable2 = new Mock<ILateUpdatable>();

            var deltaTime = 0.5f;

            testObject.RegisterLateUpdatable(mockUpdateable1.Object);
            testObject.RegisterLateUpdatable(mockUpdateable2.Object);

            testObject.LateUpdate(deltaTime);

            mockUpdateable1.Verify(m => m.LateUpdate(deltaTime), Times.Once());
            mockUpdateable2.Verify(m => m.LateUpdate(deltaTime), Times.Once());
        }

        [Fact]
        public void removing_a_late_updateable_means_it_is_no_longer_updated()
        {
            Init();

            var mockUpdateable = new Mock<ILateUpdatable>();

            var deltaTime = 0.5f;

            testObject.RegisterLateUpdatable(mockUpdateable.Object);
            testObject.UnregisterLateUpdatable(mockUpdateable.Object);

            testObject.LateUpdate(deltaTime);

            mockUpdateable.Verify(m => m.LateUpdate(deltaTime), Times.Never());
        }


        [Fact]
        public void registering_end_of_frame_updateable_twice_causes_exception()
        {
            Init();

            var mockUpdateable = new Mock<IEndOfFrameUpdatable>();

            testObject.RegisterEndOfFrameUpdatable(mockUpdateable.Object);

            Assert.Throws<FormattedException>(() =>
                testObject.RegisterEndOfFrameUpdatable(mockUpdateable.Object)
            );
        }

        [Fact]
        public void end_of_frame_also_updates_registered_end_of_frame_updateables()
        {
            Init();

            var mockUpdateable1 = new Mock<IEndOfFrameUpdatable>();
            var mockUpdateable2 = new Mock<IEndOfFrameUpdatable>();

            testObject.RegisterEndOfFrameUpdatable(mockUpdateable1.Object);
            testObject.RegisterEndOfFrameUpdatable(mockUpdateable2.Object);

            testObject.EndOfFrame();

            mockUpdateable1.Verify(m => m.EndOfFrame(), Times.Once());
            mockUpdateable2.Verify(m => m.EndOfFrame(), Times.Once());
        }

        [Fact]
        public void removing_a_end_of_frame_updateable_means_it_is_no_longer_called()
        {
            Init();

            var mockUpdateable = new Mock<IEndOfFrameUpdatable>();

            testObject.RegisterEndOfFrameUpdatable(mockUpdateable.Object);
            testObject.UnregisterEndOfFrameUpdatable(mockUpdateable.Object);

            testObject.EndOfFrame();

            mockUpdateable.Verify(m => m.EndOfFrame(), Times.Never());
        }

        [Fact]
        public void registering_renderable_twice_causes_exception()
        {
            Init();

            var mockRenderable = new Mock<IRenderable>();

            testObject.RegisterRenderable(mockRenderable.Object);

            Assert.Throws<FormattedException>(() =>
                testObject.RegisterRenderable(mockRenderable.Object)
            );
        }

        [Fact]
        public void app_update_also_calls_render_on_renderables()
        {
            Init();

            var mockRenderable1 = new Mock<IRenderable>();
            var mockRenderable2 = new Mock<IRenderable>();

            testObject.RegisterRenderable(mockRenderable1.Object);
            testObject.RegisterRenderable(mockRenderable2.Object);

            testObject.Render();

            mockRenderable1.Verify(m => m.Render(), Times.Once());
            mockRenderable2.Verify(m => m.Render(), Times.Once());
        }

        [Fact]
        public void removing_an_renderable_means_it_is_no_longer_rendered()
        {
            Init();

            var mockRenderable = new Mock<IRenderable>();

            testObject.RegisterRenderable(mockRenderable.Object);
            testObject.UnregisterRenderable(mockRenderable.Object);

            testObject.Render();

            mockRenderable.Verify(m => m.Render(), Times.Never());
        }
    }
}
