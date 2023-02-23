using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;

public class GameStateTDL : MonoBehaviour
{
    public State[] state;
    public State finalState;
    public State startState;
    public Transform agent;
    
    
    public GameObject debugFloor;
    public GameObject debugText;

    private List<GameObject> floors;
    private List<GameObject> texts;

    public int nbEp;
    [Range(0.0f, 1.0f)]
    public float alpha;
    [Range(0.0f, 1.0f)]
    public float gamma;
    [Range(0.0f, 1.0f)]
    public float epsilon;
    
    public enum TDL_TYPE // your custom enumeration
    {
        SARSA,
        QLearning,
    };
    
    public TDL_TYPE algo = TDL_TYPE.SARSA; 
    // Start is called before the first frame update
    void Start()
    {
        state = GameManager.Instance().GetStates();
        floors = new List<GameObject>();
        texts = new List<GameObject>();

        startState = state[3 + 16 * 10];
        finalState = state[9 + 16 * 5];

        if (algo == TDL_TYPE.SARSA)
        {
            SARSA(nbEp, alpha,gamma);
        }
        else
        {
            QLearning(nbEp, alpha,gamma);
        }
        
        Debug();
        StartCoroutine(Move());
    }


    bool EpsilonGreedyDecay(float pInit, float pFinal, int nbEpisodes, int currentEpisode)
    {
        float r = Mathf.Max((nbEpisodes - currentEpisode) / nbEpisodes, 0);
        float epsilon = (pInit - pFinal) * r + pFinal;
        float p = Random.Range(0.0f, 1.0f);

        return p < epsilon;
    }
    
    bool EpsilonGreedy()
    {
        float p = Random.Range(0.0f, 1.0f);

        return p < epsilon;
    }
    
    IEnumerator Move()
    {
        var current = startState;
        while (current.policy != null)
        {
            yield return new WaitForSeconds(1);
            //Debug.Log(current);
            var dir = Vector3.zero;
            switch (current.policy.dir)
            {
                case Direction.Up:
                    dir = new Vector3(-1, 0, 0);
                    break;
                case Direction.Down:
                    dir = new Vector3(1, 0, 0);
                    break;
                case Direction.Right:
                    dir = new Vector3(0, 0, 1);
                    break;
                case Direction.Left:
                    dir = new Vector3(0, 0, -1);
                    break;
            }
            agent.position += dir;
            current = current.policy.nextState;
        }
    }
    
    void Debug()
    {
        floors.ForEach(x => Destroy(x));
        floors.Clear();
        texts.ForEach(x => Destroy(x));
        texts.Clear();

        var grid = GameManager.Instance().GetGrid();
        
        for (int i = 0; i < grid.Length; i++)
        {
            var go = Instantiate(debugFloor, grid[i].transform.position, Quaternion.Euler(0, 0, 0));
            /*if (state[i].policy == null)
            {
                go.GetComponent<MeshRenderer>().material.color = new Color(state[i].Vs, 0, 0);
                floors.Add(go);
                continue;
            }
            go.GetComponent<MeshRenderer>().material.color = new Color(0,state[i].policy.Qs, 0);
            floors.Add(go);
            go = Instantiate(debugText, grid[i].transform.position, Quaternion.Euler(90, 0, 90));
            go.GetComponent<TextMeshPro>().text = String.Format("{0:0.###}", state[i].policy.Qs);
            texts.Add(go);*/
        }
    }

    void SARSA(int nbEpisodes, float alpha, float gamma)
    {
        //Init
        foreach (var s in state)
        {
            /*foreach (var action in s.actions)
            {
                action.Qs = 0;
            }*/
            if (s != null && s.actions.Count > 0)
            {
                s.policy = s.actions[Random.Range(0, s.actions.Count)];
            }
        }
        
        //
        for (int i = 0; i < nbEpisodes; i++)
        {
            State current;
            current = startState;
            Action a;
            if (EpsilonGreedyDecay(0.7f, 0.3f, nbEpisodes, i))
            {
                a = current.actions[Random.Range(0, current.actions.Count - 1)];
            }
            else
            {
                a = current.policy;
            }

            var T = 0;
            while (T < 10000)
            {
                if (current == finalState)
                {
                    break;
                }
                float r = a.reward;
                State sPrime = a.nextState;
                if (sPrime == finalState)
                {
                    break;
                }
                Action aPrime;
                if (EpsilonGreedyDecay(0.7f, 0.3f, nbEpisodes, i))
                {
                    var otherAction = sPrime.actions.Where(x => x != sPrime.policy).ToList();
                    if (otherAction.Count == 0)
                    {
                        otherAction = sPrime.actions;
                    }
                    aPrime = otherAction[Random.Range(0,  otherAction.Count - 1)];
                }
                else
                {
                    aPrime = sPrime.policy;
                }
                a.Qs += alpha * (r + gamma * (aPrime.Qs) - a.Qs);
                current = sPrime;
                a = aPrime;
                T += 1;
            }
        }
        
        foreach (var x in state)
        {
            if (x == null || x.actions.Count == 0)
            {
                continue;
            }
            x.policy = x.actions.OrderByDescending(y => y.Qs).First();
        }
    }
    
    void QLearning(int nbEpisodes, float alpha, float gamma)
    {
        //Init
        foreach (var s in state)
        {
            /*foreach (var action in s.actions)
            {
                action.Qs = 0;
            }*/
            if (s.actions.Count > 0)
            {
                s.policy = s.actions[Random.Range(0, s.actions.Count)];
            }
        }
        
        //
        for (int i = 0; i < nbEpisodes; i++)
        {
            State current;
            current = startState;
            Action a;
            if (EpsilonGreedyDecay(0.7f, 0.3f, nbEpisodes, i))
            {
                a = current.actions[Random.Range(0, current.actions.Count - 1)];
            }
            else
            {
                a = current.policy;
            }
            
            var T = 0;
            while (T < 10000)
            {
                if (current == finalState)
                {
                    break;
                }
                float r = a.reward;
                State sPrime = a.nextState;
                if (sPrime == finalState)
                {
                    break;
                }
                Action aPrime;
                if (EpsilonGreedyDecay(0.7f, 0.3f, nbEpisodes, i))
                {
                    var otherAction = sPrime.actions.Where(x => x != sPrime.policy).ToList();
                    if (otherAction.Count == 0)
                    {
                        otherAction = sPrime.actions;
                    }
                    aPrime = otherAction[Random.Range(0,  otherAction.Count - 1)];
                }
                else
                {
                    aPrime = sPrime.policy;
                }
                float target = sPrime.actions.OrderByDescending(x => x.Qs).First().Qs;
                a.Qs += alpha * (r + gamma * target - a.Qs);
                current = sPrime;
                a = aPrime;
                T += 1;
            }
        }
        
        foreach (var x in state)
        {
            if (x.actions.Count == 0)
            {
                continue;
            }
            x.policy = x.actions.OrderByDescending(y => y.Qs).First();
        }
    }
}
