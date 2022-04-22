using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Linq;
using UnityEngine.SceneManagement;
public class Controller : MonoBehaviour
{
	[SerializeField]
	protected Transform indicator;
	protected Camera mainCam;

	protected GameObject[] dudes;
	protected int dudeIndex = 0;
	protected Dude selectedDude;
	protected Color originalColor;
	protected TMP_Text selectedDudeName;
	protected TMP_Text agendaText;
	protected TMP_Text specialText;
	protected GameTracker tracker;

	protected bool isGoing = false;
	private void Start()
	{
		mainCam = Camera.main;
		tracker = GetComponent<GameTracker>();
		dudes = GameObject.FindGameObjectsWithTag("Dude");
		selectedDudeName = GameObject.Find("SelectedDude").GetComponent<TMP_Text>();
		agendaText = GameObject.Find("AgendaText").GetComponent<TMP_Text>();
		specialText = GameObject.Find("SpecialText").GetComponent<TMP_Text>();
		SelectDude(dudes[dudeIndex].GetComponent<Dude>());
	}

	void SelectDude(Dude dude)
	{
		foreach(GameObject go in dudes)
		{
			go.GetComponent<Dude>().HideGuns();
			go.GetComponentInChildren<TMP_Text>().color = Color.white;
		}
		if (selectedDude != null && !selectedDude.isDead)
			selectedDude.GetComponent<SpriteRenderer>().color = originalColor;
		selectedDude = dude;
		agendaText.text = "";
		specialText.text = dude.specialText;
		if (selectedDude != null && !selectedDude.isDead)
		{
			originalColor = selectedDude.GetComponent<SpriteRenderer>().color;
			selectedDude.GetComponent<SpriteRenderer>().color = Color.cyan;
			selectedDudeName.text = selectedDude.gameObject.name;
			selectedDude.ShowGuns();


			var agenda = tracker.agenda[selectedDude.gameObject.name];
			foreach (var agendum in agenda)
			{
				if(agendum != null)
					agendaText.text += agendum.ToString() + "\n";
			}

			if (selectedDude.team == AgendaTeam.Outlaws)
			{
				foreach (GameObject go in dudes)
				{
					if(go.GetComponent<Dude>().team == AgendaTeam.Outlaws)
					{
						go.GetComponentInChildren<TMP_Text>().color = Color.red;
					}
				}
			}
		}

	}
	// Update is called once per frame
	void Update()
	{
		if (Input.GetKeyDown(KeyCode.J))
		{
			SceneManager.LoadScene(0);
			return;
		}
		if (isGoing)
			return;
		if (Input.GetButtonDown("Next"))
		{
			dudeIndex = (dudeIndex + 1) % dudes.Length;
			SelectDude(dudes[dudeIndex].GetComponent<Dude>());
			
		} else if (Input.GetButtonDown("Previous"))
		{
			dudeIndex = (dudes.Length + dudeIndex - 1) % dudes.Length;
			SelectDude(dudes[dudeIndex].GetComponent<Dude>());
		}
		if (Input.GetKeyDown(KeyCode.Space))
		{
			Go();
			tracker.NewRound();
		}
		RaycastHit hit;
		if (Physics.Raycast(mainCam.ScreenPointToRay(Input.mousePosition), out hit, 100f))
		{
			if (hit.collider.tag == "Dude")
			{
				if (!hit.collider.GetComponent<Dude>().isDead)
				{
					indicator.position = hit.transform.position;
					if (selectedDude != null)
					{
						if (Input.GetMouseButtonDown(0))
						{
							selectedDude.SelectTarget1(hit.transform);
						}
						if (Input.GetMouseButtonDown(1))
						{
							selectedDude.SelectTarget2(hit.transform, selectedDude.role != Role.Prostitute && selectedDude.role != Role.Clairvoyant && selectedDude.role != Role.Gentleman);
							specialText.text = selectedDude.specialText;
						}
						return;
					}
				}
			}
		}
		indicator.position = Vector3.one * 10000f;
		if (Input.GetMouseButtonDown(0))
		{
			selectedDude.SelectTarget1(null);
		} 
		else if (Input.GetMouseButtonDown(1))
		{
			selectedDude.SelectTarget2(null, selectedDude.role != Role.Prostitute && selectedDude.role != Role.Clairvoyant && selectedDude.role != Role.Gentleman);
		}
	}
	void Go()
	{
		foreach (GameObject go in dudes)
		{
			go.GetComponent<Dude>().ShowGuns();
			go.GetComponent<Dude>().Go();
		}
		isGoing = true;
		StartCoroutine("WaitForTurn");
	}

	IEnumerator WaitForTurn()
	{
		yield return new WaitForSeconds(10f);
		isGoing = false;
		dudes = GameObject.FindGameObjectsWithTag("Dude").Where(go => !go.GetComponent<Dude>().isDead).ToArray();
		tracker.RoundOver();
		agendaText.text = "";
		var agenda = tracker.agenda[selectedDude.gameObject.name];
		foreach (var agendum in agenda)
		{
			agendaText.text += agendum.ToString() + "\n";
		}
	}
}
