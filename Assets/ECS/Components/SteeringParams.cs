using Unity.Entities;

namespace ECS.Components
{
    public struct SteeringParams : IComponentData
    {
    	public float CellSize;         // размер ячейки spatial hash (обычно = 2*средний радиус)
    	public float NeighborRadius;   // радиус поиска соседей
    	public int   MaxNeighbors;     // ограничение числа соседей для расчёта
    	public float AvoidWeight;      // вес отталкивания
    	public float TargetWeight;     // вес притяжения к игроку
    	public float MaxSpeed;         // целевая скорость зомби
    	public float TurnRate;         // насколько быстро корректируем направление (ед/с)
    	public float StopVelEps;       // порог "стоячий" (для метрики), м/с
    }
}