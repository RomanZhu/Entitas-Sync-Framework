using NetStack.Serialization;

public static class PackEntityUtility
{
    public static void Pack(GameEntity e, BitBuffer buffer)
    {
		ushort counter = 0;

		var hasId = false;
        if(e.hasId)
		{
			hasId = true;
			counter++;
		}

			var hasCharacter = false;
        if(e.isCharacter)
		{
			hasCharacter = true;
			counter++;
		}

			var hasControlledBy = false;
        if(e.hasControlledBy)
		{
			hasControlledBy = true;
			counter++;
		}

			var hasConnection = false;
        if(e.hasConnection)
		{
			hasConnection = true;
			counter++;
		}

			var hasSync = false;
        if(e.isSync)
		{
			hasSync = true;
			counter++;
		}

	
		buffer.AddUShort(counter);

        if (hasId)
        {
            e.id.Serialize(buffer);
        }

	        if (hasCharacter)
        {
            buffer.AddUShort(1);
        }

	        if (hasControlledBy)
        {
            e.controlledBy.Serialize(buffer);
        }

	        if (hasConnection)
        {
            e.connection.Serialize(buffer);
        }

	        if (hasSync)
        {
            buffer.AddUShort(4);
        }

		}
}
