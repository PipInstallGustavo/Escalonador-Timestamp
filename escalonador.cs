using System;
using System.Collections.Generic;
using System.Linq;

public class Escalonador
{
    private Dictionary<string, string> timestamps;
    private List<Tuple<string, string>> escalonamentos;

    public Escalonador(Dictionary<string, string> timestamps, List<Tuple<string, string>> escalonamentos)
    {
        this.timestamps = timestamps;
        this.escalonamentos = escalonamentos;
    }

    public List<string> Executar()
    {
        var resultados = new List<string>();
        // Normaliza chaves de timestamp para minúsculas e converte valores
        var TS = timestamps
            .ToDictionary(kv => kv.Key.ToLower(), kv => int.Parse(kv.Value));

        foreach (var esc in escalonamentos)
        {
            string nome = esc.Item1;
            var ops = esc.Item2.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            // Identifica dados
            var dados = new HashSet<string>();
            foreach (var op in ops)
            {
                int p1 = op.IndexOf('(');
                int p2 = op.IndexOf(')');
                if (p1 >= 0 && p2 > p1)
                    dados.Add(op.Substring(p1 + 1, p2 - p1 - 1));
            }

            var readTS = dados.ToDictionary(d => d, d => 0);
            var writeTS = dados.ToDictionary(d => d, d => 0);

            bool rollback = false;

            for (int i = 0; i < ops.Length; i++)
            {
                var op = ops[i];
                char tipo = op[0];
                if (tipo == 'c' || tipo == 'a') continue;

                // Determina chave da transação em minúsculas, ex: 't1'
                string idRaw = op.Length > 1 ? op[1].ToString() : string.Empty;
                string transacao = "t" + idRaw;

                int p1 = op.IndexOf('(');
                int p2 = op.IndexOf(')');
                string dado = (p1 >= 0 && p2 > p1) ? op.Substring(p1 + 1, p2 - p1 - 1) : string.Empty;

                // Valida leitura
                if (tipo == 'r')
                {
                    if (!TS.ContainsKey(transacao) || TS[transacao] < writeTS[dado])
                    {
                        resultados.Add($"{nome}-ROLLBACK-{i}"); rollback = true; break;
                    }
                    readTS[dado] = Math.Max(readTS[dado], TS[transacao]);
                }
                // Valida escrita
                else if (tipo == 'w')
                {
                    if (!TS.ContainsKey(transacao) || TS[transacao] < readTS[dado] || TS[transacao] < writeTS[dado])
                    {
                        resultados.Add($"{nome}-ROLLBACK-{i}"); rollback = true; break;
                    }
                    writeTS[dado] = TS[transacao];
                }
            }

            if (!rollback)
                resultados.Add($"{nome}-OK");
        }

        return resultados;
    }
} 