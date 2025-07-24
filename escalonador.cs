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
        // Lista para armazenar os resultados que serão gravados no out.txt
        var resultados = new List<string>();
        // Normaliza chaves de timestamp para minúsculas e converte valores se preciso
        var TS = timestamps
            .ToDictionary(kv => kv.Key.ToLower(), kv => int.Parse(kv.Value));

        foreach (var esc in escalonamentos)
        {
            // Extrai o nome do escalonamento e as operações
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

            for (int momento = 0; momento < ops.Length; momento++)
            {
                var op = ops[momento];
                char tipo = op[0];

                // Identifica transação e dado
                string idRaw = op.Length > 1 && char.IsDigit(op[1]) ? op[1].ToString() : string.Empty;
                string transacao = !string.IsNullOrEmpty(idRaw) ? "t" + idRaw : string.Empty;


                // Extrai dado acessado
                int d1 = op.IndexOf('(');
                int d2 = op.IndexOf(')');
                string dado = (d1 >= 0 && d2 > d1) ? op.Substring(d1 + 1, d2 - d1 - 1) : string.Empty;
                // COMMIT
                if (tipo == 'c')
                {
                    // Reniciliaza a estrutura de TS de todos os objetos de dado
                    // Timestamp Read e Timestamp Write são zerados
                    foreach (var dadoKey in dados)
                    {
                        estruturaTS[dadoKey] = (0, 0);
                    }
                }
                if (tipo == 'r')
                {
                    // Se a transação não existe ou o timestamp da transação é menor que o timestamp de escrita do dado, faz rollback
                    if (!TS.ContainsKey(transacao) || TS[transacao] < GetTSWrite(dado))
                    {
                        resultados.Add($"{nome}-ROLLBACK-{momento}");
                        rollback = true;
                        RegistrarOperacaoNoArquivo(dado, nome, "READ", momento);
                        break;
                    }
                    // Atualiza o timestamp de leitura do dado
                    AtualizarTSRead(dado, TS[transacao]);
                    RegistrarOperacaoNoArquivo(dado, nome, "READ", momento);
                    PrintEstruturaTSIntermediario(nome, momento);
                }
                
                else if (tipo == 'w')
                {
                    // Se a transação não existe ou o timestamp da transação é menor que o timestamp de leitura ou escrita do dado, faz rollback
                    if (!TS.ContainsKey(transacao) || TS[transacao] < GetTSRead(dado) || TS[transacao] < GetTSWrite(dado))
                    {
                        resultados.Add($"{nome}-ROLLBACK-{momento}");
                        rollback = true;
                        RegistrarOperacaoNoArquivo(dado, nome, "WRITE", momento);
                        break;
                    }
                    // Atualiza o timestamp de escrita do dado
                    AtualizarTSWrite(dado, TS[transacao]);
                    RegistrarOperacaoNoArquivo(dado, nome, "WRITE", momento);
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
