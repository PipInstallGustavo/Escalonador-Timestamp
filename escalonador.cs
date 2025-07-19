using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class Escalonador
{
    public Dictionary<string, string> timestamps;
    public List<Tuple<string, string>> escalonamentos;
    public List<string> res = new List<string>();
    public int momento = 0;

    // Estrutura <ID-dado, TS-Read, TS-Write>
    private Dictionary<string, (int TSRead, int TSWrite)> estruturaTS;

    public Escalonador(Dictionary<string, string> timestamps, List<Tuple<string, string>> escalonamentos)
    {
        this.timestamps = timestamps;
        this.escalonamentos = escalonamentos;
    }

    private void InicializarEstruturaTS(IEnumerable<string> dados)
    {
        estruturaTS = new Dictionary<string, (int, int)>();
        foreach (var dado in dados)
            estruturaTS[dado] = (0, 0);
    }

    private int GetTSRead(string dado) => estruturaTS.ContainsKey(dado) ? estruturaTS[dado].TSRead : 0;
    private int GetTSWrite(string dado) => estruturaTS.ContainsKey(dado) ? estruturaTS[dado].TSWrite : 0;

    private void AtualizarTSRead(string dado, int ts)
    {
        if (estruturaTS.ContainsKey(dado))
            estruturaTS[dado] = (Math.Max(estruturaTS[dado].TSRead, ts), estruturaTS[dado].TSWrite);
    }

    private void AtualizarTSWrite(string dado, int ts)
    {
        if (estruturaTS.ContainsKey(dado))
            estruturaTS[dado] = (estruturaTS[dado].TSRead, ts);
    }

    //printar cada momento da estrutura para manter o tracking
    private void PrintEstruturaTSIntermediario(string nomeEscalonamento, int momento)
    {
        Console.WriteLine($"[Momento {momento}] {nomeEscalonamento} - Estado atual da estrutura:");
        foreach (var kvp in estruturaTS)
            Console.WriteLine($"<{kvp.Key}, {kvp.Value.TSRead}, {kvp.Value.TSWrite}>");
        Console.WriteLine();
    }

    // Método para registrar a operação num arquivo do dado
    private void RegistrarOperacaoNoArquivo(string dado, string escalonamento, string operacao, int momento)
    {
        string nomeArquivo = dado + ".txt";
        string linha = $"{escalonamento} - {operacao} - {momento}";
        File.AppendAllText(nomeArquivo, linha + Environment.NewLine);
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

            // Inicializa a estrutura <ID, TS-Read, TS-Write>
            InicializarEstruturaTS(dados);

            bool rollback = false;
            for (int i = 0; i < ops.Length; i++)
            {
                var op = ops[i];
                char tipo = op[0];

                if (tipo == 'c' || tipo == 'a') continue;

                momento++;
                // Determina chave da transação em minúsculas, ex: 't1'
                string idRaw = op.Length > 1 ? op[1].ToString() : string.Empty;
                string transacao = "t" + idRaw;

                int p1 = op.IndexOf('(');
                int p2 = op.IndexOf(')');
                string dado = (p1 >= 0 && p2 > p1) ? op.Substring(p1 + 1, p2 - p1 - 1) : string.Empty;

                // Valida leitura
                if (tipo == 'r')
                {
                    if (!TS.ContainsKey(transacao) || TS[transacao] < GetTSWrite(dado))
                    {
                        resultados.Add($"{nome}-ROLLBACK-{i}"); rollback = true; break;
                    }
                    // debug
                    res.Add(dado + ", " + nome + ", READ, " + momento);

                    AtualizarTSRead(dado, TS[transacao]);
                    RegistrarOperacaoNoArquivo(dado, nome, "READ", momento);
                    PrintEstruturaTSIntermediario(nome, momento); // log após leitura
                }
                // Valida escrita
                else if (tipo == 'w')
                {
                    if (!TS.ContainsKey(transacao) || TS[transacao] < GetTSRead(dado) || TS[transacao] < GetTSWrite(dado))
                    {
                        resultados.Add($"{nome}-ROLLBACK-{i}"); rollback = true; break;
                    }
                    //debug
                    res.Add(dado + ", " + nome + ", WRITE, " + momento);
                    AtualizarTSWrite(dado, TS[transacao]);
                    RegistrarOperacaoNoArquivo(dado, nome, "WRITE", momento);
                    PrintEstruturaTSIntermediario(nome, momento); // log após escrita
                }
            }

            if (!rollback)
                resultados.Add($"{nome}-OK");
        }

        Console.WriteLine("res: " + string.Join(", ", res));
        return resultados;
    }
}
