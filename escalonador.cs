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

    // Método para printar o estado final da estrutura após um escalonamento
    private void PrintEstruturaTSFinal(string nomeEscalonamento)
    {
        Console.WriteLine($"--- Estado Final após {nomeEscalonamento} ---");
        foreach (var kvp in estruturaTS)
            Console.WriteLine($"<{kvp.Key}, {kvp.Value.TSRead}, {kvp.Value.TSWrite}>");
        Console.WriteLine("-------------------------------------------\n");
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

            // Identifica todos os dados envolvidos neste escalonamento
            var dados = new HashSet<string>();
            foreach (var op in ops)
            {
                int p1 = op.IndexOf('(');
                int p2 = op.IndexOf(')');
                if (p1 >= 0 && p2 > p1)
                    dados.Add(op.Substring(p1 + 1, p2 - p1 - 1));
            }

            // Inicializa a estrutura <ID, TS-Read, TS-Write> para cada dado
            InicializarEstruturaTS(dados);

            bool rollback = false;
            var endedTransactions = new HashSet<string>(); // transações que já foram COMMIT
            int momento = 0; // Zera o contador de momento para este escalonamento

            for (int i = 0; i < ops.Length; i++)
            {
                var op = ops[i];
                char tipo = op[0];
                momento++; // pós-incremento

                // Identifica transação e dado
                string idRaw = op.Length > 1 && char.IsDigit(op[1]) ? op[1].ToString() : string.Empty;
                string transacao = !string.IsNullOrEmpty(idRaw) ? "t" + idRaw : string.Empty;

                

            

                // Se a transação já encerrou (commit ou abort), pular
                if (!string.IsNullOrEmpty(transacao) && endedTransactions.Contains(transacao))
                {
                    // ignora operações de tX após commit/abort
                    continue;
                }

                // Extrai dado acessado
                int d1 = op.IndexOf('(');
                int d2 = op.IndexOf(')');
                string dado = (d1 >= 0 && d2 > d1) ? op.Substring(d1 + 1, d2 - d1 - 1) : string.Empty;
                // COMMIT
                if (tipo == 'c')
                {
                    RegistrarOperacaoNoArquivo(dado, transacao, "COMMIT", momento);
                    endedTransactions.Add(transacao);
                    continue; // continua processamento do escalonamento, mas ignora tX daqui pra frente
                }
                if (tipo == 'r')
                {
                    if (!TS.ContainsKey(transacao) || TS[transacao] < GetTSWrite(dado))
                    {
                        resultados.Add($"{nome}-ROLLBACK-{i}");
                        rollback = true;
                        RegistrarOperacaoNoArquivo(dado, transacao, "READ", momento);
                        break;
                    }
                    AtualizarTSRead(dado, TS[transacao]);
                    RegistrarOperacaoNoArquivo(dado, transacao, "READ", momento);
                    PrintEstruturaTSIntermediario(nome, momento);
                }
                else if (tipo == 'w')
                {
                    if (!TS.ContainsKey(transacao) || TS[transacao] < GetTSRead(dado) || TS[transacao] < GetTSWrite(dado))
                    {
                        resultados.Add($"{nome}-ROLLBACK-{i}");
                        rollback = true;
                        RegistrarOperacaoNoArquivo(dado, transacao, "WRITE", momento);
                        break;
                    }
                    AtualizarTSWrite(dado, TS[transacao]);
                    RegistrarOperacaoNoArquivo(dado, transacao, "WRITE", momento);
                    PrintEstruturaTSIntermediario(nome, momento);
                }
            }

            if (!rollback)
                resultados.Add($"{nome}-OK");

            PrintEstruturaTSFinal(nome);
        }

        return resultados;
    }

}
