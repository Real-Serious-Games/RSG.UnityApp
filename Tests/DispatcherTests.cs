using Moq;
using RSG.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace RSG.Tests
{
    public class DispatcherTests
    {
        Mock<ILogger> mockLogger;

        Dispatcher testObject;

        void Init()
        {
            mockLogger = new Mock<ILogger>();

            testObject = new Dispatcher(mockLogger.Object);
        }

        [Fact]
        public void invoke_async_is_not_executed_immediately()
        {
            Init();

            var invoked = false;

            testObject.InvokeAsync(() => invoked = true);

            Assert.False(invoked);
        }

        [Fact]
        public void can_execute_async_action()
        {
            Init();

            var invoked = false;

            testObject.InvokeAsync(() => invoked = true);

            testObject.ExecutePending();

            Assert.True(invoked);
        }

        [Fact]
        public void can_execute_muiltiple_async_actions()
        {
            Init();

            var count = 0;

            testObject.InvokeAsync(() => ++count);
            testObject.InvokeAsync(() => ++count);
            testObject.InvokeAsync(() => ++count);

            testObject.ExecutePending();

            Assert.Equal(3, count);
        }

        [Fact]
        public void async_action_during_async_execution_is_invoked_immediately()
        {
            Init();

            var invoked = false;

            testObject.InvokeAsync(() =>
                testObject.InvokeAsync(() => invoked = true)
            );

            testObject.ExecutePending();

            Assert.True(invoked);
        }

    }
}
