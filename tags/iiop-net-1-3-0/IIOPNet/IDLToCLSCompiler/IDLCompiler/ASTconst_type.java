/* Generated By:JJTree: Do not edit this line. ASTconst_type.java */

package parser;

public class ASTconst_type extends SimpleNode {
  public ASTconst_type(int id) {
    super(id);
  }

  public ASTconst_type(IDLParser p, int id) {
    super(p, id);
  }


  /** Accept the visitor. **/
  public Object jjtAccept(IDLParserVisitor visitor, Object data) {
    return visitor.visit(this, data);
  }
}