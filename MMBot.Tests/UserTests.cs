using System.Collections.Generic;
using System.Linq;
using Common.Logging;
using Xunit;

namespace MMBot.Tests
{

    public class UserTests
    {
        [Fact]
        public void WhenUserDoesNotExist_IsInRoleReturnsFalse()
        {
            var robot = new RobotBuilder(new LoggerConfigurator(LogLevel.All))
                        .UseAdapter<StubAdapter>()
                        .UseBrain<StubBrain>()
                        .DisablePluginDiscovery()
                        .Build();

            Assert.False(robot.IsInRole("foo", "theRole"));
        }

        [Fact]
        public void WhenUserDoesHaveAnotherRole_IsInRoleReturnsFalse()
        {
            var robot = new RobotBuilder(new LoggerConfigurator(LogLevel.All))
                        .UseAdapter<StubAdapter>()
                        .UseBrain<StubBrain>()
                        .DisablePluginDiscovery()
                        .Build();

            robot.AddUserToRole("foo", "anotherRole");

            Assert.False(robot.IsInRole("foo", "theRole"));
        }
        
        [Fact]
        public void WhenUserDoesHaveRole_IsInRoleReturnsTrue()
        {
            var robot = new RobotBuilder(new LoggerConfigurator(LogLevel.All))
                        .UseAdapter<StubAdapter>()
                        .UseBrain<StubBrain>()
                        .DisablePluginDiscovery()
                        .Build();

            robot.AddUserToRole("foo", "theRole");

            Assert.True(robot.IsInRole("foo", "theRole"));
        }


        [Fact]
        public void WhenUserHasRoles_GetUserRoles_ReturnsRoles()
        {
            var robot = new RobotBuilder(new LoggerConfigurator(LogLevel.All))
                        .UseAdapter<StubAdapter>()
                        .UseBrain<StubBrain>()
                        .DisablePluginDiscovery()
                        .Build();

            robot.AddUserToRole("foo", "theRole");
            robot.AddUserToRole("foo", "anotherRole");

            Assert.Equal(2, robot.GetUserRoles("foo").Count());
        }

        [Fact]
        public void WhenAddingMultipleRolesAtOnce_GetUserRoles_ReturnsRoles()
        {
            var robot = new RobotBuilder(new LoggerConfigurator(LogLevel.All))
                        .UseAdapter<StubAdapter>()
                        .UseBrain<StubBrain>()
                        .DisablePluginDiscovery()
                        .Build();

            robot.AddUserToRole("foo", new[]{"theRole", "anotherRole"});

            Assert.Equal(2, robot.GetUserRoles("foo").Count());
        }

        [Fact]
        public void WhenUserDoesNotHaveRole_RemoveUserFromRoleDoesNotThrow()
        {
            var robot = new RobotBuilder(new LoggerConfigurator(LogLevel.All))
                        .UseAdapter<StubAdapter>()
                        .UseBrain<StubBrain>()
                        .DisablePluginDiscovery()
                        .Build();

            robot.AddUserToRole("foo", new[] { "theRole", "anotherRole" });
            
            robot.RemoveUserFromRole("foo", "notTheRoles");

            Assert.Equal(2, robot.GetUserRoles("foo").Count());
        }

        [Fact]
        public void WhenUserDoesHaveRole_RemoveUserFromRoleRemovesRole()
        {
            var robot = new RobotBuilder(new LoggerConfigurator(LogLevel.All))
                        .UseAdapter<StubAdapter>()
                        .UseBrain<StubBrain>()
                        .DisablePluginDiscovery()
                        .Build();

            robot.AddUserToRole("foo", new[] { "theRole", "anotherRole" });

            robot.RemoveUserFromRole("foo", "theRole");

            Assert.Equal(1, robot.GetUserRoles("foo").Count());

            Assert.Equal("anotherRole", robot.GetUserRoles("foo").First());
        }

        [Fact]
        public void WhenUserHasNoAliases_GetUserAliases_ReturnsEmptyList()
        {
            var robot = new RobotBuilder(new LoggerConfigurator(LogLevel.All))
            .UseAdapter<StubAdapter>()
            .UseBrain<StubBrain>()
            .DisablePluginDiscovery()
            .Build();

            var aliases = robot.GetUserAliases("foo");

            Assert.NotNull(aliases);
            Assert.Equal(0, aliases.Length);

        }

        [Fact]
        public void WhenUserHasAliases_GetUserAliases_ReturnsAliases()
        {
            var robot = new RobotBuilder(new LoggerConfigurator(LogLevel.All))
            .UseAdapter<StubAdapter>()
            .UseBrain<StubBrain>()
            .DisablePluginDiscovery()
            .Build();

            var expectedAlias = "The Foo";
            robot.RegisterAliasForUser("foo", expectedAlias);

            var aliases = robot.GetUserAliases("foo");

            Assert.NotNull(aliases);
            Assert.Equal(1, aliases.Length);
            Assert.Equal(expectedAlias, aliases[0]);

        }

        [Fact]
        public void WhenUserHasAlias_GetUserAliasesByAlias_ReturnsUsername()
        {
            var robot = new RobotBuilder(new LoggerConfigurator(LogLevel.All))
            .UseAdapter<StubAdapter>()
            .UseBrain<StubBrain>()
            .DisablePluginDiscovery()
            .Build();

            var alias= "The Foo";
            var username = "foo";
            robot.RegisterAliasForUser(username, alias);

            var aliases = robot.GetUserAliases(alias);

            Assert.NotNull(aliases);
            Assert.Equal(1, aliases.Length);
            Assert.Equal(username, aliases[0]);

        }

        [Fact]
        public void WhenUserHasRolesAndAnAlias_IsInRoleByAlias_ReturnsTrue()
        {
            var robot = new RobotBuilder(new LoggerConfigurator(LogLevel.All))
            .UseAdapter<StubAdapter>()
            .UseBrain<StubBrain>()
            .DisablePluginDiscovery()
            .Build();

            var alias = "The Foo";
            var username = "foo";
            string role = "The Role";

            robot.RegisterAliasForUser(username, alias);
            
            robot.AddUserToRole(username, role);

            Assert.True(robot.IsInRole(alias, role));

        }
    }
}