/* Generated By:JJTree: Do not edit this line. ASTraises_expr.java */

package parser;

public class ASTraises_expr extends SimpleNode {
  public ASTraises_expr(int id) {
    super(id);
  }

  public ASTraises_expr(IDLParser p, int id) {
    super(p, id);
  }


  /** Accept the visitor. **/
  public Object jjtAccept(IDLParserVisitor visitor, Object data) {
    return visitor.visit(this, data);
  }
}
