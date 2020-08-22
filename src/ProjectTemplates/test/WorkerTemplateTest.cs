// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Templates.Test.Helpers;
using Xunit;
using Xunit.Abstractions;
using Microsoft.AspNetCore.Testing;

namespace Templates.Test
{
    public class WorkerTemplateTest
    {
        public WorkerTemplateTest(ProjectFactoryFixture projectFactory, ITestOutputHelper output)
        {
            ProjectFactory = projectFactory;
            Output = output;
        }

        public Project Project { get; set; }
        public ProjectFactoryFixture ProjectFactory { get; }
        public ITestOutputHelper Output { get; }

        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Linux, SkipReason = "https://github.com/dotnet/sdk/issues/12831")]
        [TheoryData("C#")]
        [TheoryData("F#")]
        public async Task WorkerTemplateAsync(string language)
        {
            var projectName = "worker"
            Project = await ProjectFactory.GetOrCreateProject(
                $"worker-{ language.ToLowerInvariant()[0] }sharp",
                Output);

            var createResult = await Project.RunDotNetNewAsync("worker", language: language);
            Assert.True(0 == createResult.ExitCode, ErrorMessages.GetFailedProcessMessage("create/restore", Project, createResult));

            var publishResult = await Project.RunDotNetPublishAsync();
            Assert.True(0 == publishResult.ExitCode, ErrorMessages.GetFailedProcessMessage("publish", Project, publishResult));

            // Run dotnet build after publish. The reason is that one uses Config = Debug and the other uses Config = Release
            // The output from publish will go into bin/Release/netcoreappX.Y/publish and won't be affected by calling build
            // later, while the opposite is not true.

            var buildResult = await Project.RunDotNetBuildAsync();
            Assert.True(0 == buildResult.ExitCode, ErrorMessages.GetFailedProcessMessage("build", Project, buildResult));

            using (var aspNetProcess = Project.StartBuiltProjectAsync(hasListeningUri: false))
            {
                Assert.False(
                    aspNetProcess.Process.HasExited,
                    ErrorMessages.GetFailedProcessMessageOrEmpty("Run built project", Project, aspNetProcess.Process));
            }

            using (var aspNetProcess = Project.StartPublishedProjectAsync(hasListeningUri: false))
            {
                Assert.False(
                    aspNetProcess.Process.HasExited,
                    ErrorMessages.GetFailedProcessMessageOrEmpty("Run published project", Project, aspNetProcess.Process));
            }
        }
    }
}
