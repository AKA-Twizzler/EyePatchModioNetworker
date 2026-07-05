using LabFusion.Player;
using ModioModNetworker.Data;

public class DownloadQueueElement
{
	public PlayerID associatedPlayer;

	public ModInfo info;

	public bool notify = true;

	public bool lobby = false;
}
