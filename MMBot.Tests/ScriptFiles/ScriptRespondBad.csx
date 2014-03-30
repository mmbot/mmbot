var robot = Require<Robot>();

robot.Respond("test", msg => msg.Send("Bad"));