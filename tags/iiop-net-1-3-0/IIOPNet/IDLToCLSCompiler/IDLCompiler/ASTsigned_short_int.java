/* Generated By:JJTree: Do not edit this line. ASTsigned_short_int.java */

package parser;

public class ASTsigned_short_int extends SimpleNode {
  public ASTsigned_short_int(int id) {
    super(id);
  }

  public ASTsigned_short_int(IDLParser p, int id) {
    super(p, id);
  }


  /** Accept the visitor. **/
  public Object jjtAccept(IDLParserVisitor visitor, Object data) {
    return visitor.visit(this, data);
  }
}