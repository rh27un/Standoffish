using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;

public enum Role
{
	Sheriff,
	Deputy,
	Stranger,
	Barkeep,
	Prostitute,
	Gambler,
	Pianist,
	Clairvoyant,
	Zorro,
	Butcher,
	Gentleman,
	Cowboy
}

public class Dude : MonoBehaviour
{
	[SerializeField]
	protected Transform startTarget1;
	[SerializeField]
	protected Transform startTarget2;

	protected Transform target1;
	protected Transform target2;
	[SerializeField]
	protected float shootForce;

	protected Vector3 target1Pos;
	protected Vector3 target2Pos;

	protected Transform gun1;
	protected Transform gun2;

	protected List<Dude> pointedBy = new List<Dude>();

	public bool isDead;
	public bool isGoing;
	public bool sheriffDead;

	protected GameTracker tracker;

	public Role role;

	protected TMP_Text nameText;
	protected TMP_Text roleText;

	public AgendaTeam team = AgendaTeam.Town;

	public string specialText = string.Empty;
	protected bool isShooting;

	protected int hp = 1;
	private void Awake()
	{
		gun1 = transform.GetChild(0);
		gun2 = transform.GetChild(1);
		nameText = transform.Find("Name").GetComponent<TMP_Text>();
		roleText = transform.Find("Role").GetComponent<TMP_Text>();
		nameText.text = gameObject.name;
		HideGuns();
		tracker = Camera.main.GetComponent<GameTracker>();
		//SelectTarget1(startTarget1);
		//SelectTarget2(startTarget2);
	}
	// Update is called once per frame
	void Update()
	{
		if (Input.GetKeyDown(KeyCode.W))
		{
			if(isGoing && role == Role.Sheriff || (role == Role.Deputy && sheriffDead))
			{
				Shoot();
			}
		}
    }

	public void SetRole(Role newRole)
	{
		role = newRole;
		roleText.text = role.ToString();
		if(newRole == Role.Butcher)
		{
			hp = 2;
		}
	}

	public void SelectTarget1(Transform newTarget, bool magic = true)
	{

		if (target1 != null)
		{
			if (!magic)
				return;
			target1.GetComponent<Dude>().BeUnselected(this);
		}
		target1 = newTarget;
		if(newTarget != null)
		{
			gun1.gameObject.SetActive(true);
			target1.GetComponent<Dude>().BeSelected(this);
			target1Pos = target1.position;
			gun1.LookAt(target1Pos);
			var line = gun1.GetComponentInChildren<LineRenderer>();
			line.enabled = true;
			line.SetPosition(0, gun1.position);
			line.SetPosition(1, target1Pos);
		} else
		{
			gun1.gameObject.SetActive(false);
		}
	}
	public void SelectTarget2(Transform newTarget, bool magic = false)
	{

		if (target2 != null)
		{
			if (!magic)
				return;
			if (role == Role.Gentleman)
			{
				specialText = "";
				target2.GetComponent<Dude>().hp--;
				target2.GetComponent<Dude>().specialText = string.Empty;
			}
			target2.GetComponent<Dude>().BeUnselected(this);
		}
		target2 = newTarget;
		switch (role)
		{
			case Role.Gentleman:
				if(newTarget != null)
				{
					Dude due = newTarget.GetComponent<Dude>();
					specialText = $"Now protecting {due.gameObject.name}";
					due.specialText += $"Being protected by {gameObject.name}";
					
					due.hp++;
				}
				break;
			case Role.Clairvoyant:
				if(newTarget != null)
				{
					var agenda = tracker.agenda.SelectMany(a => a.Value).Where(a => a.Agendee == target2.gameObject.name && a.state == AgendaState.Active).ToList();
					if(agenda.Count() > 0)
					{
						var agendum = agenda[Random.Range(0, agenda.Count())];
						var agender = tracker.agenda.Single(a => a.Value.Contains(agendum)).Key;

						switch (agendum.agendaType)
						{
							case AgendaType.Kill:
							case AgendaType.SeeDead:
								specialText = $"{target2.gameObject.name} is being targeted by {agender}";
								break;
							case AgendaType.Protect:
								specialText = $"{target2.gameObject.name} is being protected by {agender}";
								break;
						}
					} else
					{
						specialText = $"{target2.gameObject.name}'s future seems very bland";
					}
				} else
				{
					specialText = string.Empty;
				}
				break;
			case Role.Prostitute:
				if(newTarget != null)
				{
					var agenda = tracker.agenda[target2.gameObject.name].Where(a => a.state == AgendaState.Active).ToList();
					var agendum = agenda[Random.Range(0, agenda.Count())];

					if (agendum != null)
					{
						switch (agendum.agendaType)
						{
							case AgendaType.Kill:
							case AgendaType.SeeDead:
								specialText = $"{target2.gameObject.name} might have it out for {agendum.Agendee}";
								break;
							case AgendaType.Protect:
								specialText = $"{target2.gameObject.name} seems to be looking out for {agendum.Agendee}";
								break;
						}
					}
				} else
				{
					specialText = string.Empty;
				}
				break;
			default:
				if (newTarget != null)
				{
					gun2.gameObject.SetActive(true);
					target2.GetComponent<Dude>().BeSelected(this);
					target2Pos = target2.position;
					gun2.LookAt(target2Pos);
					var line = gun2.GetComponentInChildren<LineRenderer>();
					line.enabled = true;
					line.SetPosition(0, gun2.position);
					line.SetPosition(1, target2Pos);
				}
				break;
		}
		if(newTarget == null)
		{
			gun2.gameObject.SetActive(false);
		}
	}

	public void BeSelected(Dude by)
	{
		pointedBy.Add(by);
	}

	public void BeUnselected(Dude by)
	{
		pointedBy.Remove(by);
		if (isGoing && (pointedBy.Count < 1 || (role == Role.Stranger && pointedBy.Where(p => p.transform != target1 && p.transform != target2).Count() < 1)))
		{
			switch (role)
			{
				case Role.Sheriff:
					return;
				case Role.Deputy:
					if (sheriffDead)
						return;
					Shoot();
					return;
				default:
					Shoot();
					return;
			}
		}
	}

	public void Shoot()
	{
		StartCoroutine("ShootEffects");
	}

	IEnumerator ShootEffects()
	{
		if (!isDead && !isShooting)
		{
			isShooting = true;
			if (role == Role.Stranger)
				yield return new WaitForSeconds(0.1f);
			else if(!(role == Role.Sheriff || (role == Role.Deputy && sheriffDead)))
				yield return new WaitForSeconds(Random.Range(0.1f, 0.5f));
			if (target1 != null)
			{
				gun1.GetComponentInChildren<ParticleSystem>().Play();
				gun1.GetComponentInChildren<AudioSource>().Play();
				target1.GetComponent<Dude>().Die((target1Pos - transform.position).normalized * shootForce, gameObject.name);
			}
			if(target2 != null && role != Role.Prostitute && role != Role.Clairvoyant && role != Role.Gentleman)
			{
				gun2.GetComponentInChildren<ParticleSystem>().Play();
				gun2.GetComponentInChildren<AudioSource>().Play();
				target2.GetComponent<Dude>().Die((target2Pos - transform.position).normalized * shootForce, gameObject.name);
			}
			var lines = GetComponentsInChildren<LineRenderer>();
			foreach (var line in lines)
			{
				line.enabled = false;
			}
			foreach (var dude in pointedBy)
			{
				dude.Shoot();
			}
			yield return new WaitForSeconds(2f);
			if (target1 != null)
			{
				SelectTarget1(null);
			}
			if (target2 != null)
			{
				SelectTarget2(null);
			}
			isShooting = false;
		}
	}
	public void Die(Vector3 direction, string killer)
	{
		if (!isDead)
		{
			hp--;
			if (hp <= 0)
			{
				isDead = true;
				GetComponent<SpriteRenderer>().color = Color.grey;
				GetComponent<Rigidbody>().AddForce(direction, ForceMode.Impulse);
				transform.rotation = Quaternion.Euler(new Vector3(90f, 0f, Vector3.Angle(Vector3.right, -direction)));
				transform.GetChild(4).LookAt(transform.position - direction);
				transform.GetChild(4).GetComponent<ParticleSystem>().Play();
				nameText.enabled = false;
				roleText.enabled = false;
				HideGuns();
				if (target1 != null)
					target1.GetComponent<Dude>().BeUnselected(this);
				if (target2 != null)
					target2.GetComponent<Dude>().BeUnselected(this);
				tracker.RecordDeath(gameObject.name, killer);
			}
		}
	}

	public void Go()
	{
		isGoing = true;
		if (pointedBy.Where(p => p.role != Role.Gambler).Count() < 1 || (role == Role.Stranger && pointedBy.Where(p => p.role != Role.Gambler).Where(p => p.transform != target1 && p .transform != target2).Count() < 1)) 
		{
			switch (role)
			{
				case Role.Sheriff:
					break;
				case Role.Deputy:
					if (sheriffDead)
						break;
					Shoot();
					break;
				default:
					Shoot();
					break;
			}
		}

		StartCoroutine("Gone");
	}

	IEnumerator Gone()
	{
		yield return new WaitForSeconds(3f);
		isGoing = false;
		if(target1 != null && target1.GetComponent<Dude>().isDead)
		{
			SelectTarget1(null, true);
		}
		if (target2 != null && target2.GetComponent<Dude>().isDead || role == Role.Prostitute || role == Role.Clairvoyant || role == Role.Gentleman)
		{
			SelectTarget2(null, true);
		}
	}
	public void HideGuns()
	{
		gun1.gameObject.SetActive(false);
		gun2.gameObject.SetActive(false);
	}

	public void ShowGuns()
	{
		gun1.gameObject.SetActive(target1 != null);
		gun2.gameObject.SetActive(target2 != null && role != Role.Prostitute && role != Role.Clairvoyant && role != Role.Gentleman);
	}

	public bool HasTargets()
	{
		return (target1 != null || target2 != null);
	}
}
