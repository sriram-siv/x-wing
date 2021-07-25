LogMove should capture objects as they exist in the game

Previous position and rotation

Last move as ShipConfig.Maneuver { speed, direction, difficulty, direction }

This should go in a global log file - undo & redo available (if player owns ship)
This should also log hazards and bombs

Each move should get a unique id - a simple incrementing int should do

Drop template should look at the ships own movement flags
