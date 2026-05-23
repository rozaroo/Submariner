public interface IButtonControls : IControls
{
    public bool isPressed { get; set; }
    public void OnActionUp();
    public void OnActionDown();
}