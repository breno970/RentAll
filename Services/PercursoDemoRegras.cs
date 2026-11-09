using aluguel.Models;

namespace aluguel.Services;

/// <summary>
/// Server-side state transitions for a DEMO rental. Validates the real authenticated
/// participant role and refuses invalid transitions. There are no Stripe side effects.
/// </summary>
public static class PercursoDemoRegras
{
    public const int MaxNota = 500;
    public const int MaxAcessorios = 300;
    public static readonly IReadOnlyDictionary<string, string> EstadosArtigo = new Dictionary<string, string>
    {
        ["como_novo"] = "Como novo",
        ["bom"] = "Bom estado",
        ["marcas_uso"] = "Marcas de uso",
        ["danificado"] = "Danificado / com defeito"
    };

    /// <summary>Operations whose detail only exists in the logistics trail table.</summary>
    public static bool ExigeHistorico(string operacao) =>
        operacao is "pedir_alteracao" or "divergencia" or "checklist_entrega" or "checklist_devolucao";

    public static string Atualizar(PercursoAluguerDemo p, string operacao, bool anunciante,
        DateTime agoraUtc, string? metodo = null, string? tipo = null,
        string? local = null, DateTime? entregaUtc = null, int? avaliacao = null,
        string? nota = null, string? estadoArtigo = null, string? acessorios = null)
    {
        string estado = p.Estado;
        void Exigir(bool condicao, string mensagem)
        {
            if (!condicao) throw new InvalidOperationException(mensagem);
        }
        nota = string.IsNullOrWhiteSpace(nota) ? null : nota.Trim();
        acessorios = string.IsNullOrWhiteSpace(acessorios) ? null : acessorios.Trim();
        Exigir(nota == null || nota.Length <= MaxNota, $"As observações podem ter até {MaxNota} caracteres.");
        Exigir(acessorios == null || acessorios.Length <= MaxAcessorios,
            $"A lista de acessórios pode ter até {MaxAcessorios} caracteres.");
        switch (operacao)
        {
            case "pedir_alteracao":
                Exigir(!anunciante, "Só o locatário pode pedir alterações à proposta de entrega.");
                Exigir(estado == "EntregaProposta", "Não existe uma proposta de entrega para alterar.");
                Exigir(nota is { Length: >= 3 }, "Explica que alteração pretendes (mínimo 3 caracteres).");
                p.Estado = "PagamentoSimulado";
                p.AcordadaEm = null;
                break;
            case "checklist_entrega":
                Exigir(estado == "EntregaAcordada",
                    "A checklist de entrega é registada depois de a entrega ser acordada e antes de ser confirmada.");
                Exigir(estadoArtigo != null && EstadosArtigo.ContainsKey(estadoArtigo),
                    "Indica o estado do artigo.");
                break;
            case "checklist_devolucao":
                Exigir(estado is "EmCurso" or "DevolucaoPendente",
                    "A checklist de devolução só pode ser registada durante o aluguer ou a devolução.");
                Exigir(estadoArtigo != null && EstadosArtigo.ContainsKey(estadoArtigo),
                    "Indica o estado do artigo.");
                break;
            case "divergencia":
                Exigir(anunciante, "A divergência na devolução é registada pelo anunciante.");
                Exigir(estado == "DevolucaoPendente" && !p.IncidenteAberto,
                    "Só podes registar uma divergência enquanto a devolução está por confirmar.");
                Exigir(nota is { Length: >= 3 }, "Descreve a divergência encontrada (mínimo 3 caracteres).");
                p.IncidenteAberto = true;
                break;
            case "aprovar":
            case "recusar":
                Exigir(!anunciante, "Só o locatário pode avançar no checkout de demonstração.");
                Exigir(estado is "Checkout" or "PagamentoRecusado",
                    "Este checkout já foi tratado. Consulta o estado atual.");
                Exigir(metodo is "cartao" or "mbway", "Seleciona um método demonstrativo válido.");
                p.MetodoPagamento = metodo;
                if (operacao == "aprovar")
                {
                    p.Estado = "PagamentoSimulado";
                    p.PagamentoEm = agoraUtc;
                }
                else p.Estado = "PagamentoRecusado";
                break;
            case "propor":
                Exigir(anunciante, "A proposta de entrega cabe ao anunciante.");
                Exigir(estado is "PagamentoSimulado" or "EntregaProposta",
                    "A entrega só pode ser proposta após o checkout fictício.");
                Exigir(tipo is "em_maos" or "ponto_recolha" or "transportadora",
                    "Seleciona uma modalidade de entrega.");
                local = local?.Trim();
                Exigir(!string.IsNullOrWhiteSpace(local) && local.Length is >= 3 and <= 120,
                    "Indica um local público ou ponto de recolha com 3 a 120 caracteres.");
                Exigir(entregaUtc.HasValue && entregaUtc.Value > agoraUtc.AddMinutes(15)
                    && entregaUtc.Value < agoraUtc.AddDays(90),
                    "A data da entrega deve estar entre 15 minutos e 90 dias no futuro.");
                p.EntregaTipo = tipo;
                p.EntregaLocal = local;
                p.EntregaEm = entregaUtc;
                p.AcordadaEm = null;
                p.Estado = "EntregaProposta";
                break;
            case "aceitar":
                Exigir(!anunciante, "Só o locatário pode aceitar a entrega.");
                Exigir(estado == "EntregaProposta" && p.EntregaEm.HasValue,
                    "Não existe uma proposta de entrega a aceitar.");
                p.Estado = "EntregaAcordada";
                p.AcordadaEm = agoraUtc;
                break;
            case "entregar":
                Exigir(anunciante, "Só o anunciante pode confirmar a entrega.");
                Exigir(estado == "EntregaAcordada" && !p.EntreguePeloAnuncianteEm.HasValue,
                    "A entrega já foi confirmada ou ainda não foi acordada.");
                p.EntreguePeloAnuncianteEm = agoraUtc;
                if (p.RecebidaPeloLocatarioEm.HasValue) p.Estado = "EmCurso";
                break;
            case "receber":
                Exigir(!anunciante, "Só o locatário pode confirmar a receção.");
                Exigir(estado == "EntregaAcordada" && !p.RecebidaPeloLocatarioEm.HasValue,
                    "A receção já foi confirmada ou ainda não foi acordada.");
                p.RecebidaPeloLocatarioEm = agoraUtc;
                if (p.EntreguePeloAnuncianteEm.HasValue) p.Estado = "EmCurso";
                break;
            case "devolver":
                Exigir(!anunciante, "Só o locatário pode registar a devolução.");
                Exigir(estado == "EmCurso" && !p.IncidenteAberto,
                    "Não podes avançar enquanto o aluguer não estiver em curso ou existir uma ocorrência.");
                p.DevolvidaPeloLocatarioEm = agoraUtc;
                p.Estado = "DevolucaoPendente";
                break;
            case "concluir":
                Exigir(anunciante, "Só o anunciante pode confirmar o recebimento da devolução.");
                Exigir(estado == "DevolucaoPendente" && !p.IncidenteAberto,
                    "A devolução ainda não está pendente ou existe uma ocorrência aberta.");
                p.RecebidaPeloAnuncianteEm = agoraUtc;
                p.Estado = "Concluido";
                break;
            case "incidente":
                Exigir(estado is "EmCurso" or "DevolucaoPendente",
                    "Uma ocorrência só pode ser aberta durante o aluguer ou devolução.");
                Exigir(!p.IncidenteAberto, "Já existe uma ocorrência de demonstração aberta.");
                p.IncidenteAberto = true;
                break;
            case "resolver":
                Exigir((estado is "EmCurso" or "DevolucaoPendente") && p.IncidenteAberto,
                    "Não existe ocorrência aberta para resolver.");
                Exigir(anunciante, "A resolução demonstrativa deve ser confirmada pelo anunciante.");
                p.IncidenteAberto = false;
                break;
            case "cancelar":
                Exigir(!anunciante, "Só o locatário pode cancelar este percurso.");
                Exigir(estado is "PagamentoSimulado" or "EntregaProposta",
                    "Este percurso já foi acordado, entregue ou cancelado.");
                p.Estado = "Cancelado";
                break;
            case "avaliar":
                Exigir(!anunciante, "A avaliação demonstrativa pertence ao locatário.");
                Exigir(estado == "Concluido" && !p.Avaliacao.HasValue,
                    "A avaliação ainda não está disponível ou já foi atribuída.");
                Exigir(avaliacao is >= 1 and <= 5, "Seleciona uma avaliação entre 1 e 5.");
                p.Avaliacao = avaliacao;
                break;
            default:
                throw new InvalidOperationException("Operação não reconhecida.");
        }
        p.AtualizadoEm = agoraUtc;
        p.Versao++;
        return operacao switch
        {
            "pedir_alteracao" => "Pedido de alteração enviado. O anunciante pode propor nova data, hora ou local.",
            "checklist_entrega" => "Checklist de entrega guardada no histórico partilhado.",
            "checklist_devolucao" => "Checklist de devolução guardada no histórico partilhado.",
            "divergencia" => "Divergência registada. A conclusão fica suspensa até ser resolvida.",
            "aprovar" => "Aprovação simulada guardada. Não foi cobrado dinheiro.",
            "recusar" => "Recusa simulada guardada. Podes repetir o checkout.",
            "propor" => "Proposta de entrega guardada. O locatário pode aceitá-la.",
            "aceitar" => "Entrega acordada. Ambas as partes podem confirmar a entrega.",
            "entregar" or "receber" => "Confirmação guardada. O percurso avança quando ambas as partes confirmarem.",
            "devolver" => "Devolução registada. Aguarda confirmação do anunciante.",
            "concluir" => "Aluguer demonstrativo concluído. Não houve pagamento real.",
            "incidente" => "Ocorrência fictícia registada. O fecho fica suspenso.",
            "resolver" => "Ocorrência resolvida e registada no histórico.",
            "cancelar" => "Cancelamento e reembolso fictícios registados. Não houve movimentos financeiros.",
            "avaliar" => "Avaliação demonstrativa guardada.",
            _ => "Operação guardada."
        };
    }
}
