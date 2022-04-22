using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Linq;
public enum AgendaType
{
	Kill,
	SeeDead,
	Protect
}

public enum AgendaState
{
	Active,
	Passed,
	Failed
}

public enum AgendaTeam
{
	Town,
	Outlaws,
	Family
}
public class Agenda
{
	public string specialText;
	public string Agendee;
	public AgendaType agendaType;
	public AgendaState state = AgendaState.Active;
	public AgendaTeam team;
	public override string ToString()
	{
		string str = "";
		if (!string.IsNullOrEmpty(specialText))
			str = specialText;
		else
		{
			switch (agendaType)
			{
				case AgendaType.Kill:
					str = $"Kill {Agendee}";
					break;
				case AgendaType.SeeDead:
					str = $"See {Agendee} Dead";
					break;
				case AgendaType.Protect:
					str = $"Protect {Agendee}";
					break;
			}
		}
		switch (state)
		{
			case AgendaState.Passed:
				str += " (passed)";
				break;
			case AgendaState.Failed:
				str += " (failed)";
				break;
		}
		return str;
	}
}
public class GameTracker : MonoBehaviour
{
	protected TMP_Text gameLog;
	protected GameObject[] dudes;
	public Dictionary<string, List<Agenda>> agenda = new Dictionary<string, List<Agenda>>();
	public GameObject mexicanStandoffObject;

	bool deathsThisRound;
	public int numOutlaws;
	public int numFamily;
	public int numPlayers;
	protected int[] outlaws;
	protected int[] family;

	protected List<int> roles; // Key is player id, Value is role id
	[SerializeField]
	[Range(0f, 1f)]
	protected float specialRoleRatio; // + l + no bitches + touch grass
	protected int deputyThreshold;
	protected List<int> possibleRoles = new List<int>() { 2, 3, 4, 5, 6, 7, 8, 9, 10 };
	private void Awake()
	{
		gameLog = GameObject.Find("GameLog").GetComponent<TMP_Text>();
		dudes = GetDudes();
		GenerateRoles();
		outlaws = new int[numOutlaws];
		int l = 0;
		for(int i = 0; i < numOutlaws; i++)
		{
			l = Random.Range(l + 1, dudes.Length - numOutlaws + i);
			outlaws[i] = l;
		}
		for(int i = 0; i < dudes.Length; i++)
		{
			Role role;
			role = (Role)roles[i];
			dudes[i].GetComponent<Dude>().SetRole(role);
			if (outlaws.Contains(i))
			{
				dudes[i].GetComponent<Dude>().team = AgendaTeam.Outlaws;
				GenerateAgendas(i, role, AgendaTeam.Outlaws);
			}
			else
			{
				GenerateAgendas(i, role, AgendaTeam.Town);
			}
		}

	}

	void GenerateAgendas(int dude, Role role, AgendaTeam team)
	{
		if (team == AgendaTeam.Town)
		{
			if (role == Role.Sheriff || role == Role.Deputy)
			{
				var agendas = new List<Agenda>();
				for (int i = 0; i < numOutlaws; i++)
				{
					agendas.Add(new Agenda() { Agendee = dudes[outlaws[i]].name, agendaType = AgendaType.SeeDead, specialText = "Kill the outlaws" });
				}
				agenda.Add(dudes[dude].name, agendas);
			} 
			else
			{
				int agendee1 = Random.Range(1, dudes.Length - 1);
				int agendee2 = Random.Range(agendee1 + 1, dudes.Length);
				var agendas = new List<Agenda>() {
					new Agenda() { Agendee = dudes[(dude + agendee1) % dudes.Length].name, agendaType = AgendaType.Kill },
					new Agenda() { Agendee = dudes[(dude + agendee1) % dudes.Length].name, agendaType = AgendaType.SeeDead },
					new Agenda() { Agendee = dudes[(dude + agendee2) % dudes.Length].name, agendaType = AgendaType.Protect },
				};
				agenda.Add(dudes[dude].name, agendas);
			}
		}
		else if(team == AgendaTeam.Outlaws)
		{
			var agendas = new List<Agenda>()
			{
				new Agenda() { Agendee = dudes[(int)Role.Sheriff].name, agendaType = AgendaType.SeeDead, specialText = "Kill the sheriff"},
			};
			if (!outlaws.Contains((int)Role.Deputy))
			{
				agendas.Add(new Agenda() { Agendee = dudes[(int)Role.Deputy].name, agendaType = AgendaType.SeeDead, specialText = "Kill the deputy" });
			}
			agendas.Add(new Agenda() { Agendee = dudes[dude].name, agendaType = AgendaType.Protect, specialText = "Survive" });
			agenda.Add(dudes[dude].name, agendas);
		}

	}

	void GenerateRoles()
	{
		roles = new List<int>()
		{
			0 // Sheriff always exists
		};
		if (numPlayers >= deputyThreshold)
			roles.Add(1);
		int numSpecial = Mathf.FloorToInt(numPlayers * specialRoleRatio);
		for(int i = roles.Count; i < numPlayers; i++)
		{
			if (i <= numSpecial && possibleRoles.Count > 0)
			{
				int toAdd = possibleRoles[Random.Range(0, possibleRoles.Count)];
				possibleRoles.Remove(toAdd);
				roles.Add(toAdd);
			}
			else
			{
				roles.Add(11);
			}
		}
	}

	public void NewRound()
	{
		deathsThisRound = false;
	}

	public void RoundOver()
	{
		if (!deathsThisRound)
		{
			bool mexicanStandoff = false;
			foreach (var dude in dudes)
			{
				var comp = dude.GetComponent<Dude>();
				if (!comp.isDead)
				{
					if (comp.HasTargets())
					{
						mexicanStandoff = true;
						break;
					}
				}
			}
			if (!mexicanStandoff)
			{
				GameOver();
			} else
			{
				StartCoroutine(DisplayText("Mexican Standoff!!"));
			}
		}
	}

	void GameOver()
	{
		StartCoroutine(DisplayText("Situation Defused!"));
		foreach (var agendas in agenda)
		{
			foreach (var agendum in agendas.Value)
			{
				if (agendum.agendaType == AgendaType.Protect)
				{
					if (!GameObject.Find(agendum.Agendee).GetComponent<Dude>().isDead)
						agendum.state = AgendaState.Passed;
				}
			}
		}
	}

	IEnumerator DisplayText(string text)
	{
		mexicanStandoffObject.GetComponent<TMP_Text>().text = text;
		mexicanStandoffObject.SetActive(true);
		yield return new WaitForSeconds(5f);
		mexicanStandoffObject.SetActive(false);
	}
	public void RecordDeath(string dead, string killer)
	{
		gameLog.text += $"{dead} was killed by {killer}\n";
		foreach(var agendum in agenda[killer])
		{
			if(agendum.agendaType == AgendaType.Kill && agendum.Agendee == dead)
			{
				agendum.state = AgendaState.Passed;
			}
		}
		foreach(var agendas in agenda)
		{
			bool vengeance = false;
			foreach(var agendum in agendas.Value)
			{
				if(agendum.agendaType == AgendaType.Kill && agendum.Agendee == dead && agendas.Key != killer)
				{
					agendum.state = AgendaState.Failed;
				}
				if(agendum.agendaType == AgendaType.SeeDead && agendum.Agendee == dead)
				{
					agendum.state = AgendaState.Passed;
				}
				else if(agendum.agendaType == AgendaType.Protect && agendum.Agendee == dead)
				{
					agendum.state = AgendaState.Failed;
					vengeance = true;
				}
			}
			if(vengeance)
				agendas.Value.Add(new Agenda { Agendee = killer, agendaType = AgendaType.Kill });
		}
		deathsThisRound = true;
		if(dudes[(int)Role.Sheriff].name == dead)
		{
			dudes[(int)Role.Deputy].GetComponent<Dude>().sheriffDead = true;
		}
	}

	GameObject[] GetDudes()
	{
		var initialDudes = GameObject.FindGameObjectsWithTag("Dude");
		GameObject[] newDudes = new GameObject[numPlayers];

		for(int i = 0; i < numPlayers; i++)
		{
			int dude = Random.Range(0, initialDudes.Length);
			while (newDudes.Contains(initialDudes[dude]))
			{
				dude = (dude + 1) % initialDudes.Length;
			}
			newDudes[i] = initialDudes[dude];
		}
		foreach (var dude in initialDudes)
		{
			if (!newDudes.Contains(dude))
				dude.SetActive(false);
		}
		return newDudes;
	}
}
