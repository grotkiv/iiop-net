/* Generated By:JJTree: Do not edit this line. ASTstruct_type.java */

package parser;

public class ASTstruct_type extends SimpleNodeWithIdent {
  public ASTstruct_type(int id) {
    super(id);
  }

  public ASTstruct_type(IDLParser p, int id) {
    super(p, id);
  }


  /** Accept the visitor. **/
  public Object jjtAccept(IDLParserVisitor visitor, Object data) {
    return visitor.visit(this, data);
  }
}